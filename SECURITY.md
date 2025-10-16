# Security Hardening for eShopOnWeb

This document summarizes the key security improvements applied and how to verify them.

## What Changed
1. Secrets management
   - Azure Key Vault configuration provider is registered when AZURE_KEYVAULT_URI/AZURE_KEY_VAULT_ENDPOINT is set; environment variables are the default.
   - No secrets are committed to source; sample appsettings contain no secrets.
2. HTTPS and HSTS
   - HTTPS redirection is enforced.
   - HSTS enabled in production with preload and includeSubDomains (configurable via HSTS:*).
3. Authentication and Cookies
   - Application cookies are Secure=Always, HttpOnly=true, SameSite=Strict, 60m expiration with sliding expiration.
4. Security Headers
   - Global middleware adds CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, and Permissions-Policy.
5. Data Protection Keys
   - Persisted to file system path DP_KEYS_PATH (or ./dpkeys); encrypted with Azure Key Vault key when KeyVault:KeyIdentifier is provided.
6. Request Size Limits
   - Kestrel MaxRequestBodySize configurable via MAX_REQUEST_BODY_SIZE (default 10 MB in example).
7. Health Checks and Swagger
   - /live returns minimal status; /ready filtered by tags.
   - Swagger is enabled only in Development or when ENABLE_SWAGGER=true.
8. Error Handling and Logging
   - Production uses UseExceptionHandler; no Developer Exception Page in production.
   - PublicApi exception middleware returns sanitized ProblemDetails.

## Verify
- Set environment:
  - export ASPNETCORE_ENVIRONMENT=Production
  - export AZURE_KEYVAULT_URI="https://<your-vault>.vault.azure.net/" (optional)
  - export DP_KEYS_PATH="./dpkeys"
  - export MAX_REQUEST_BODY_SIZE=10485760
- Run Web and PublicApi:
  - dotnet run --project src/Web/Web.csproj
  - dotnet run --project src/PublicApi/PublicApi.csproj
- Check:
  - Response headers include CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy.
  - HSTS header present on HTTPS responses in production.
  - Cookies set with Secure, HttpOnly, SameSite=Strict.
  - /live returns 200 without details; /ready is available and filtered by tags.
  - Accessing a non-existent endpoint returns sanitized ProblemDetails without stack traces.
  - Swagger only loads in Development or when ENABLE_SWAGGER=true.

## Environment Variables
See .env.example for a list of supported variables.

## Notes
- For Azure deployments, mount a persistent volume for DP_KEYS_PATH.
- Ensure KeyVault secret names for SQL connection strings are configured via AZURE_SQL_*_CONNECTION_STRING_KEY.
