CoverageSummary Tool

Purpose:
- Run ReportGenerator on Cobertura coverage files
- Generate HTML coverage report in CoverageReport/
- Produce docs/coverage-summary.md with overall and per-project coverage
- Provide docs/coverage redirect to the HTML report

Prerequisites:
- .NET SDK 8.0+
- Cobertura files available at **/TestResults/Coverage/coverage.cobertura.xml

Usage:
1) Restore the local dotnet tools (installs ReportGenerator):
   dotnet tool restore

2) Build and run the CoverageSummary tool:
   dotnet build ./tools/CoverageSummary/CoverageSummary.csproj -c Release
   dotnet run --project ./tools/CoverageSummary/CoverageSummary.csproj -- --project-root . --verbose

Outputs:
- CoverageReport/index.html (HTML summary)
- docs/coverage-summary.md (markdown summary with overall and per-project coverage)
- docs/coverage/index.html (redirect page to CoverageReport/index.html)

Notes:
- The tool first attempts to parse CoverageReport/summary.json or summary.xml produced by ReportGenerator.
- If not available, it falls back to computing coverage from Cobertura XML files directly.
