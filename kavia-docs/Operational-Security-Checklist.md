# Operational Security Checklist

## Environment and Configuration
- [ ] Enforce HTTPS; set HSTS with preload and includeSubDomains in production.
- [ ] Configure Security Headers (CSP, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, Frame-Ancestors).
- [ ] Set CookiePolicy and Application Cookie: HttpOnly, Secure, SameSite=Strict.
- [ ] Configure CORS via ALLOWED_ORIGINS environment variable (HTTPS only).
- [ ] Disable or protect Swagger in production.

## Secrets and Key Management
- [ ] Remove hardcoded secrets from source (JWT key, default passwords).
- [ ] Store secrets in Azure Key Vault; integrate with DefaultAzureCredential in production.
- [ ] Persist Data Protection keys and encrypt with Key Vault (or equivalent).
- [ ] Rotate keys regularly and document rotation procedures.

## Authentication and Authorization
- [ ] Validate JWT issuer and audience; RequireHttpsMetadata in non-Dev.
- [ ] Remove [AllowAnonymous] from protected endpoints.
- [ ] Reduce token lifetimes; implement refresh tokens as needed.
- [ ] Enforce password policies and lockout; consider MFA for admin roles.

## API and Surface Area
- [ ] Implement API versioning.
- [ ] Enable rate limiting for public endpoints.
- [ ] Minimize error detail; use ProblemDetails.
- [ ] Hide PII from Swagger schemas.

## Health and Diagnostics
- [ ] Separate /live and /ready; minimal responses.
- [ ] Restrict health endpoints via network or auth.
- [ ] Centralize structured logging; enable PII redaction; capture correlation IDs.

## Data and Database
- [ ] Use least-privileged DB accounts; separate per service.
- [ ] Enforce TLS to database; encrypt at rest where possible.
- [ ] Apply migrations via CI/CD with approvals; maintain backups and test restores.

## CI/CD and Compliance
- [ ] Use CI secrets store; never log secrets.
- [ ] Enable SAST/DAST and dependency scans; gate on critical vulnerabilities.
- [ ] Document environment variables and required Key Vault secrets.
- [ ] Maintain audit trail for deployments and security changes.

## Required Environment Variables/Secrets
- Auth:JwtKey (secret)
- Auth:Issuer
- Auth:Audience
- ConnectionStrings:CatalogConnection
- ConnectionStrings:IdentityConnection
- AZURE_KEY_VAULT_ENDPOINT
- AZURE_SQL_CATALOG_CONNECTION_STRING_KEY
- AZURE_SQL_IDENTITY_CONNECTION_STRING_KEY
- KeyVault:KeyIdentifier (for Data Protection)
- ALLOWED_ORIGINS
- HSTS:* (optional)
