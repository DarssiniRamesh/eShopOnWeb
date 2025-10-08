# Test Coverage Summary for eShopOnWeb

## Date and Environment
- Date: 2025-10-08
- Environment: Local CI execution context for container "eShopOnWeb"

## Test Execution Summary
- Totals: 15 passed, 0 failed, 0 skipped
- Duration: Approximately 7 seconds for PublicApiIntegrationTests (overall suite completed within a short window as indicated by artifacts)

## Coverage Summary
Aggregated coverage based on available reports.

- Line coverage: 12.89% (741/5747)
- Branch coverage: 3.70% (114/3074)
- Notes:
  - Metrics parsed from Cobertura XML reports.
  - Coverage values reflect current test suite execution and may vary slightly between runs depending on filters and test selection.

## Artifacts
- Results directory: eShopOnWeb/TestResults/Coverage
- Cobertura XML files:
  - eShopOnWeb/TestResults/Coverage/a4fac961-dca4-4aa9-af08-dcb7712f7a09/coverage.cobertura.xml
  - eShopOnWeb/TestResults/Coverage/_aa72925c929d_2025-10-08_05_47_39/In/aa72925c929d/coverage.cobertura.xml
- TRX (test results): eShopOnWeb/TestResults/Coverage/test_results.trx
- HTML coverage: Not generated in this run

## Observations
- Overall coverage is currently low, with 12.89% line coverage and 3.70% branch coverage. This suggests significant portions of the codebase, especially conditional logic, are not exercised by the current tests.
- xUnit2013 analyzer suggestions were present, indicating opportunities to improve assertion style and clarity (e.g., using Assert.Equal instead of Assert.True/False for equality checks).
- Duplicate role warnings were observed during tests, likely related to role seeding or test setup behaviors creating redundant roles.

## Recommendations
- Generate human-readable coverage reports:
  - Use ReportGenerator to produce HTML and badge summaries from Cobertura XML.
  - Aggregate multiple Cobertura files for a single combined report.
- Prioritize adding tests in:
  - ApplicationCore (domain logic and specifications)
  - Infrastructure (repositories and data access)
  - PublicApi (endpoint behaviors and edge cases)
- Improve branch coverage by targeting condition-heavy code paths, error handling, and authorization checks.
- Clean up assertions per xUnit2013 recommendations to improve test readability and diagnostics.
- Review and stabilize role/user seeding logic in tests to avoid duplicate role warnings.
- Integrate coverage thresholds into CI to prevent regressions (e.g., fail when line coverage drops below a defined baseline).

## How to Reproduce Locally
- Run tests with coverage using the provided runsettings (recommended):
```bash
# From the eShopOnWeb folder
dotnet test --settings tests/CodeCoverage.runsettings --logger "trx;LogFileName=TestResults/Coverage/test_results.trx"
```

- Alternatively, run with inline settings (Cobertura output):
```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura --logger "trx;LogFileName=TestResults/Coverage/test_results.trx"
```

- Generate HTML coverage using ReportGenerator (optional if already installed):
```bash
# Aggregate all Cobertura XMLs under TestResults/Coverage
reportgenerator \
  -reports:"TestResults/Coverage/**/*.cobertura.xml" \
  -targetdir:"TestResults/Coverage/Report" \
  -reporttypes:Html;HtmlSummary;Badges
```

- Open the HTML summary:
```bash
# macOS/Linux
open TestResults/Coverage/Report/index.html || xdg-open TestResults/Coverage/Report/index.html || true
# Windows
start TestResults/Coverage/Report/index.html
```

## Notes
- If multiple Cobertura XML files are present (as in this run), prefer aggregating them for a single, unified view of coverage across all test projects.
