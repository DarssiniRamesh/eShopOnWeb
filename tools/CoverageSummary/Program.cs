using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

/*
  Tool: CoverageSummary
  - Restores and runs ReportGenerator to aggregate Cobertura reports
  - Generates HTML report into CoverageReport/ (Html;HtmlSummary;Badges)
  - Parses summary.json or summary.xml to compute overall and per-project coverage
  - Falls back to parsing Cobertura XMLs if summary file isn't present
  - Writes docs/coverage-summary.md and docs/coverage/index.html redirect
*/

internal static class Program
{
    private const string DefaultCoberturaGlob = "**/TestResults/Coverage/coverage.cobertura.xml";
    private const string ReportOutDir = "CoverageReport";
    private const string DocsDir = "docs";
    private const string DocsCoverageDir = "docs/coverage";
    private const string DocsCoverageRedirectFile = "docs/coverage/index.html";
    private const string DocsSummaryFile = "docs/coverage-summary.md";

    private static async Task<int> Main(string[] args)
    {
        var root = new RootCommand("Generate HTML and Markdown coverage summaries from Cobertura reports")
        {
            new Option<string>(
                name: "--project-root",
                description: "Root directory of the repository (defaults to current working directory).",
                getDefaultValue: () => Directory.GetCurrentDirectory()),
            new Option<string>(
                name: "--cobertura-glob",
                description: "Glob to find Cobertura xml files.",
                getDefaultValue: () => DefaultCoberturaGlob),
            new Option<string>(
                name: "--report-out",
                description: "Output directory for ReportGenerator HTML.",
                getDefaultValue: () => ReportOutDir),
            new Option<bool>(
                name: "--verbose",
                description: "Enable verbose logging.",
                getDefaultValue: () => false)
        };

        root.SetHandler(async (string projectRoot, string coberturaGlob, string reportOut, bool verbose) =>
        {
            try
            {
                Directory.SetCurrentDirectory(projectRoot);

                // Ensure directories exist
                EnsureDir(reportOut);
                EnsureDir(DocsDir);
                EnsureDir(DocsCoverageDir);

                // Locate Cobertura files
                var coberturaFiles = GlobFiles(projectRoot, coberturaGlob).ToList();
                if (!coberturaFiles.Any())
                {
                    Log(verbose, $"No Cobertura files found with glob: {coberturaGlob}");
                }

                // Restore the reportgenerator tool
                await RunCmd("dotnet", "tool restore", verbose);

                // Run reportgenerator
                var reportsArg = coberturaFiles.Any()
                    ? string.Join(";", coberturaFiles.Select(EscapeForReportGenerator))
                    : coberturaGlob; // let reportgenerator resolve the glob if we didn't find any
                var targetDirArg = EscapeForReportGenerator(reportOut);
                var reportTypes = "Html;HtmlSummary;Badges;JsonSummary;XmlSummary";

                var rgArgs = $"tool run reportgenerator -reports:\"{reportsArg}\" -targetdir:\"{targetDirArg}\" -reporttypes:{reportTypes} -assemblyfilters:+*";
                await RunCmd("dotnet", rgArgs, verbose);

                // Parse summary from JSON/XML produced by ReportGenerator
                var summary = ParseReportGeneratorSummary(reportOut, verbose)
                              ?? ParseCoberturaFallback(coberturaFiles, projectRoot, verbose);

                // Write markdown summary
                WriteMarkdown(summary, reportOut);

                // Write docs/coverage redirect
                WriteRedirect(reportOut);

                Console.WriteLine("Coverage report generated successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error generating coverage report: " + ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }

            return 0;
        },
        root.Children.GetByAlias("--project-root") as Option<string>,
        root.Children.GetByAlias("--cobertura-glob") as Option<string>,
        root.Children.GetByAlias("--report-out") as Option<string>,
        root.Children.GetByAlias("--verbose") as Option<bool>);

        return await root.InvokeAsync(args);
    }

    private static void EnsureDir(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void Log(bool verbose, string message)
    {
        if (verbose) Console.WriteLine(message);
    }

    private static string EscapeForReportGenerator(string path)
    {
        // ReportGenerator on different shells prefers forward slashes
        return path.Replace("\\", "/");
    }

    private static IEnumerable<string> GlobFiles(string root, string pattern)
    {
        // Basic globbing for ** pattern
        // pattern like **/TestResults/Coverage/coverage.cobertura.xml
        var normalized = pattern.Replace("\\", "/");
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        IEnumerable<string> dirs = new[] { root };

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (part == "**")
            {
                dirs = dirs.SelectMany(d =>
                {
                    try { return Directory.EnumerateDirectories(d, "*", SearchOption.AllDirectories); }
                    catch { return Array.Empty<string>(); }
                }).Prepend(root).Distinct();
            }
            else
            {
                dirs = dirs.SelectMany(d =>
                {
                    try { return Directory.EnumerateDirectories(d, part, SearchOption.TopDirectoryOnly); }
                    catch { return Array.Empty<string>(); }
                });
            }
        }

        var filePattern = parts.LastOrDefault() ?? "*";
        var files = dirs.SelectMany(d =>
        {
            try { return Directory.EnumerateFiles(d, filePattern, SearchOption.TopDirectoryOnly); }
            catch { return Array.Empty<string>(); }
        });

        return files;
    }

    private class CoverageSummary
    {
        public double OverallLinePercent { get; set; }
        public double OverallBranchPercent { get; set; }
        public List<ProjectCoverage> Projects { get; set; } = new();
    }

    private class ProjectCoverage
    {
        public string Name { get; set; } = "";
        public double LinePercent { get; set; }
        public double BranchPercent { get; set; }
    }

    private static CoverageSummary? ParseReportGeneratorSummary(string reportOut, bool verbose)
    {
        try
        {
            var jsonPath = Path.Combine(reportOut, "summary.json");
            var xmlPath = Path.Combine(reportOut, "summary.xml");

            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Overall
                var summaries = root.GetProperty("summaries");
                double overallLine = 0;
                double overallBranch = 0;
                if (summaries.GetArrayLength() > 0)
                {
                    var total = summaries.EnumerateArray().FirstOrDefault(e => e.TryGetProperty("name", out var n) && n.GetString() == "Total");
                    if (total.ValueKind != JsonValueKind.Undefined)
                    {
                        overallLine = total.GetProperty("linecoverage").GetDouble();
                        overallBranch = total.TryGetProperty("branchcoverage", out var b) ? b.GetDouble() : 0;
                    }
                    else
                    {
                        // fallback to the first element
                        var first = summaries[0];
                        overallLine = first.GetProperty("linecoverage").GetDouble();
                        overallBranch = first.TryGetProperty("branchcoverage", out var b) ? b.GetDouble() : 0;
                    }
                }

                // Per assembly/project
                var projects = new List<ProjectCoverage>();
                if (root.TryGetProperty("assemblies", out var assemblies))
                {
                    foreach (var asm in assemblies.EnumerateArray())
                    {
                        var name = asm.GetProperty("name").GetString() ?? "";
                        var line = asm.GetProperty("linecoverage").GetDouble();
                        var branch = asm.TryGetProperty("branchcoverage", out var b) ? b.GetDouble() : 0;
                        projects.Add(new ProjectCoverage { Name = name, LinePercent = line, BranchPercent = branch });
                    }
                }

                return new CoverageSummary
                {
                    OverallLinePercent = overallLine,
                    OverallBranchPercent = overallBranch,
                    Projects = projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList()
                };
            }

            if (File.Exists(xmlPath))
            {
                var x = XDocument.Load(xmlPath);
                // ReportGenerator's summary.xml structure:
                // <summary>
                //   <summary>
                //      <linecoverage>..</linecoverage> <branchcoverage>..</branchcoverage>
                //   </summary>
                //   <assemblies>
                //      <assembly name="..." linecoverage=".." branchcoverage=".." />
                //   </assemblies>
                // </summary>
                var rootEl = x.Root;
                if (rootEl == null) return null;

                var totalEl = rootEl.Element("summary");
                double overallLine = TryParseDouble(totalEl?.Element("linecoverage")?.Value);
                double overallBranch = TryParseDouble(totalEl?.Element("branchcoverage")?.Value);

                var projects = new List<ProjectCoverage>();
                var assembliesEl = rootEl.Element("assemblies");
                if (assembliesEl != null)
                {
                    foreach (var asm in assembliesEl.Elements("assembly"))
                    {
                        var name = asm.Attribute("name")?.Value ?? "";
                        var line = TryParseDouble(asm.Attribute("linecoverage")?.Value);
                        var branch = TryParseDouble(asm.Attribute("branchcoverage")?.Value);
                        projects.Add(new ProjectCoverage { Name = name, LinePercent = line, BranchPercent = branch });
                    }
                }

                return new CoverageSummary
                {
                    OverallLinePercent = overallLine,
                    OverallBranchPercent = overallBranch,
                    Projects = projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList()
                };
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to parse ReportGenerator summary: " + ex.Message);
            if (verbose) Console.Error.WriteLine(ex);
        }
        return null;
    }

    private static CoverageSummary ParseCoberturaFallback(IEnumerable<string> coberturaFiles, string projectRoot, bool verbose)
    {
        double totalCovered = 0;
        double totalValid = 0;
        var perProject = new Dictionary<string, (double covered, double valid, double bCovered, double bValid)>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in coberturaFiles)
        {
            try
            {
                var x = XDocument.Load(file);
                var coverage = x.Root;
                if (coverage == null) continue;

                // Cobertura coverage root typically has attributes:
                // lines-valid, lines-covered, branches-valid, branches-covered
                double linesValid = TryParseDouble(coverage.Attribute("lines-valid")?.Value);
                double linesCovered = TryParseDouble(coverage.Attribute("lines-covered")?.Value);
                double branchesValid = TryParseDouble(coverage.Attribute("branches-valid")?.Value);
                double branchesCovered = TryParseDouble(coverage.Attribute("branches-covered")?.Value);

                totalCovered += linesCovered;
                totalValid += linesValid;

                // Group "per project" using a heuristic: derive project name from path
                // Expect path: tests/<ProjectName>/TestResults/Coverage/coverage.cobertura.xml
                var projName = DeriveProjectNameFromCoberturaPath(file, projectRoot) ?? "Unknown";
                if (!perProject.TryGetValue(projName, out var stat))
                {
                    stat = (0, 0, 0, 0);
                }
                stat.covered += linesCovered;
                stat.valid += linesValid;
                stat.bCovered += branchesCovered;
                stat.bValid += branchesValid;
                perProject[projName] = stat;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to parse Cobertura file {file}: {ex.Message}");
                if (verbose) Console.Error.WriteLine(ex);
            }
        }

        var projects = perProject.Select(kv =>
        {
            var (c, v, bc, bv) = kv.Value;
            double l = v > 0 ? (c / v) * 100.0 : 0.0;
            double b = bv > 0 ? (bc / bv) * 100.0 : 0.0;
            return new ProjectCoverage
            {
                Name = kv.Key,
                LinePercent = Round2(l),
                BranchPercent = Round2(b)
            };
        }).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

        double overallLine = totalValid > 0 ? (totalCovered / totalValid) * 100.0 : 0.0;

        return new CoverageSummary
        {
            OverallLinePercent = Round2(overallLine),
            OverallBranchPercent = Round2(WeightedBranch(perProject)),
            Projects = projects
        };
    }

    private static string? DeriveProjectNameFromCoberturaPath(string filePath, string projectRoot)
    {
        // Try to pull project name as directory under tests/
        // e.g., eShopOnWeb/tests/UnitTests/TestResults/Coverage/coverage.cobertura.xml -> UnitTests
        try
        {
            var full = Path.GetFullPath(filePath);
            var root = Path.GetFullPath(projectRoot);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;

            var rel = Path.GetRelativePath(root, full).Replace("\\", "/");
            var parts = rel.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var testsIndex = Array.FindIndex(parts, p => string.Equals(p, "tests", StringComparison.OrdinalIgnoreCase));
            if (testsIndex >= 0 && testsIndex + 1 < parts.Length)
            {
                return parts[testsIndex + 1];
            }
        }
        catch { }
        return null;
    }

    private static double WeightedBranch(Dictionary<string, (double covered, double valid, double bCovered, double bValid)> perProject)
    {
        double bc = 0, bv = 0;
        foreach (var kv in perProject.Values)
        {
            bc += kv.bCovered;
            bv += kv.bValid;
        }
        if (bv == 0) return 0.0;
        return Round2((bc / bv) * 100.0);
    }

    private static double TryParseDouble(string? s)
    {
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
        if (double.TryParse(s, out d)) return d;
        return 0.0;
    }

    private static double Round2(double v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static async Task RunCmd(string fileName, string arguments, bool verbose)
    {
        Log(verbose, $"Running: {fileName} {arguments}");
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi)!;
        var stdOutTask = proc.StandardOutput.ReadToEndAsync();
        var stdErrTask = proc.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdOutTask, stdErrTask);
        proc.WaitForExit();

        if (verbose)
        {
            if (!string.IsNullOrWhiteSpace(stdOutTask.Result)) Console.WriteLine(stdOutTask.Result);
            if (!string.IsNullOrWhiteSpace(stdErrTask.Result)) Console.Error.WriteLine(stdErrTask.Result);
        }

        if (proc.ExitCode != 0)
        {
            throw new Exception($"Command failed: {fileName} {arguments}\n{stdOutTask.Result}\n{stdErrTask.Result}");
        }
    }

    private static void WriteMarkdown(CoverageSummary summary, string reportOut)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Coverage Summary");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine();
        sb.AppendLine($"- Overall Line Coverage: {summary.OverallLinePercent:F2}%");
        sb.AppendLine($"- Overall Branch Coverage: {summary.OverallBranchPercent:F2}%");
        sb.AppendLine();
        sb.AppendLine("## Per Project");
        sb.AppendLine();
        if (summary.Projects.Count == 0)
        {
            sb.AppendLine("_No per-project data found._");
        }
        else
        {
            foreach (var proj in summary.Projects)
            {
                sb.AppendLine($"- {proj.Name}: Line {proj.LinePercent:F2}% | Branch {proj.BranchPercent:F2}%");
            }
        }
        sb.AppendLine();
        sb.AppendLine("## Reports");
        sb.AppendLine();
        sb.AppendLine($"- HTML Report: [CoverageReport/index.html](../{reportOut}/index.html)");
        sb.AppendLine($"- Summary HTML: [CoverageReport/summary.htm](../{reportOut}/summary.htm)");
        sb.AppendLine($"- Badges: [CoverageReport/badges](../{reportOut}/badges/)");

        File.WriteAllText(DocsSummaryFile, sb.ToString());
    }

    private static void WriteRedirect(string reportOut)
    {
        var html = @"<!doctype html>
<html lang=""en"">
  <head>
    <meta charset=""utf-8"">
    <meta http-equiv=""refresh"" content=""0; url=../" + reportOut + @"/index.html"">
    <title>Coverage</title>
  </head>
  <body>
    <p>Redirecting to coverage report... <a href=""../" + reportOut + @"/index.html"">Open Coverage Report</a></p>
  </body>
</html>";
        File.WriteAllText(DocsCoverageRedirectFile, html);
    }
}
