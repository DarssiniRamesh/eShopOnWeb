# Dependency Risk Report

## Overview
This report highlights potential dependency-related risks and outlines verification steps. Due to the breadth of the solution, precise versions should be audited via the csproj files and lock files in the repository and the CI pipeline.

## Key Dependencies Observed
- ASP.NET Core 8.0 (PublicApi Dockerfile base: mcr.microsoft.com/dotnet/aspnet:8.0)
- EF Core (DbContexts in Infrastructure)
- ASP.NET Core Identity
- Swashbuckle/Swagger
- AutoMapper
- Blazored.LocalStorage

## Risks and Considerations
1. Framework and runtime alignment:
   - Ensure all projects target net8.0 and deploy on .NET 8.0 runtimes with current patches.
   - Track Microsoft advisories for .NET 8 and ASP.NET Core.

2. Swagger/Swashbuckle:
   - Keep Swashbuckle updated to patch any CVEs that expose API docs or cause code execution.
   - Do not expose Swagger in production without auth.

3. EF Core:
   - Maintain latest patch version for EF Core to prevent query-related vulnerabilities and ensure security fixes.

4. Blazored.LocalStorage:
   - Keep updated; understand that local storage is a client XSS risk amplifier.

5. Identity:
   - Keep Identity packages current; audit for known auth bypass vulnerabilities.

6. Transitive dependencies:
   - Use dotnet list package --vulnerable (or similar) in CI to detect CVEs.

## Recommended Actions
- Establish a weekly automated dependency scan:
  - dotnet list <project>.csproj package --vulnerable
  - Enable GitHub Dependabot or equivalent.
- Apply semantic versioning updates for security patches automatically with manual review for minors/majors.
- Maintain a security baseline Dockerfile digest pinning and track base image CVEs.
- CI/CD gate on critical and high vulnerabilities; require approval for waivers.

## Verification Steps
- Inventory all csproj PackageReference entries and versions across:
  - src/PublicApi/PublicApi.csproj
  - src/Web/Web.csproj
  - src/BlazorAdmin/BlazorAdmin.csproj
  - src/BlazorShared/BlazorShared.csproj
  - src/ApplicationCore/ApplicationCore.csproj
  - src/Infrastructure/Infrastructure.csproj
- Run SCA tooling (e.g., dotnet-outdated, OWASP Dependency-Check) and capture artifact logs.
- Confirm base images have no critical CVEs (use Microsoft MCR vulnerability reports).
