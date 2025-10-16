# Secure Coding Guidelines for eShopOnWeb (.NET)

## Goals
Provide practical secure coding guidance aligned with the eShopOnWeb architecture and technology stack.

## Authentication and Authorization
- Do not hardcode secrets or default passwords in code. Use environment variables and Azure Key Vault.
- Validate JWT tokens with issuer/audience; set short lifetimes and rotate keys.
- Avoid [AllowAnonymous] on endpoints intended to be protected. Use [Authorize] with policies/roles.
- For cookie auth, set HttpOnly, Secure, and SameSite=Strict. Use antiforgery for state-changing actions.

## Input Validation and Model Binding
- Use data annotations (e.g., [Required], [StringLength]) and server-side validation.
- Avoid over-posting by using specific DTOs; never bind domain entities directly from request.
- Sanitize and encode output to prevent XSS; avoid rendering untrusted HTML.

## Error Handling and Logging
- Return sanitized ProblemDetails for errors; never return internal exception messages.
- Use structured logging; avoid logging secrets or PII. Apply redaction if logging request/response metadata.
- Include correlation IDs for tracing.

## Data Protection and Secrets
- Configure Data Protection with persisted keys encrypted via Azure Key Vault.
- Use Azure Key Vault for:
  - JWT signing keys
  - Database connection strings
  - Third-party API secrets
- Avoid storing secrets in appsettings.json or source control.

## Transport Security and Headers
- Enforce HTTPS redirection; enable HSTS in production with preload and includeSubDomains.
- Add security headers:
  - Content-Security-Policy (restrict scripts to self, disallow inline, set proper sources)
  - X-Content-Type-Options: nosniff
  - X-Frame-Options or Frame-Ancestors via CSP
  - Referrer-Policy: no-referrer
  - Permissions-Policy: restrict sensitive APIs

## Swagger and API Surface
- Disable Swagger in production or protect it with auth and policies.
- Hide PII and sensitive fields from generated schemas.

## CORS
- Restrict allowed origins to trusted HTTPS origins via env config; do not use wildcards in production.
- Do not allow credentials unless necessary; if used, strictly bound origins.

## EF Core and SQL
- Use parameterized queries via EF Core; avoid raw SQL unless necessary and parameterized.
- Use least-privilege DB accounts; separate accounts per service.
- Encrypt connections and data at rest where possible.

## Blazor WebAssembly Considerations
- Do not store access tokens in Local Storage; prefer cookie-based auth or in-memory storage.
- Enforce strict CSP to mitigate XSS risks; avoid inline scripts/styles.

## Health Checks and Diagnostics
- Provide minimal health responses; segregate liveness and readiness.
- Restrict health endpoints to internal networks or require auth.

## CI/CD and Operations
- Store secrets in CI/CD secret stores; never in pipeline logs.
- Scan dependencies regularly; fail builds on critical vulnerabilities.
- Automate key rotation; maintain secure backups and restore procedures.

## Sample Snippets
See “Security-Gap-Analysis-and-Roadmap.md” for implementation samples for JWT, Data Protection, Swagger gating, and headers.
