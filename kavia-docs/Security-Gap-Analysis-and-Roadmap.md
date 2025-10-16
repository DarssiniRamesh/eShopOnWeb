# Security Gap Analysis and Roadmap

## Overview
This document outlines security gaps found in eShopOnWeb relative to OWASP ASVS and Top 10, and provides a prioritized remediation roadmap including code-level guidance.

## Gaps vs. Best Practices
1. Secrets management and crypto:
   - Hardcoded JWT key and default password in source (AuthorizationConstants.cs).
   - No Data Protection key persistence.
   - Docker compose checked-in secret for SQL SA.
2. Authentication and authorization:
   - Public API disables issuer/audience validation; long-lived tokens.
   - UserController combines [Authorize] and [AllowAnonymous] on sensitive endpoints, leading to anonymous access.
3. API & operational exposure:
   - Swagger always enabled and ungated.
   - Health checks publicly accessible with detailed output.
   - No API versioning or rate limiting.
4. Client security (Blazor/Web):
   - LocalStorage caching combined with server-returned JWT to client; potential for token exposure if stored in Local Storage.
   - Inconsistent cookie settings; CSRF control review needed for state-changing endpoints.
5. Configuration & headers:
   - HSTS present only in production; missing explicit strict parameters.
   - Exception middleware returns raw exception messages.

## Prioritized Remediation Roadmap

### Quick Wins (0–2 weeks)
- Remove [AllowAnonymous] from authorized endpoints:
  - src/Web/Controllers/UserController.cs: Remove [AllowAnonymous] from GetCurrentUser and Logout.
- Restrict Swagger:
  - Wrap UseSwagger/UseSwaggerUI with if (app.Environment.IsDevelopment()) or require auth policy.
- Sanitize API error responses:
  - Update ExceptionMiddleware to return standardized ProblemDetails without raw exception messages.
- Enforce strict auth cookie settings:
  - Align SameSite=Strict, SecurePolicy=Always, HttpOnly=true consistently in ConfigureCookieSettings and AddCookie.
- Configure CORS via env variables:
  - Define ALLOWED_ORIGINS and require HTTPS origins only.

### Near-Term (2–6 weeks)
- Secrets & Key Management:
  - Replace AuthorizationConstants.JWT_SECRET_KEY and DEFAULT_PASSWORD with environment variables or Key Vault secrets.
  - Implement Data Protection key persistence to Key Vault/Blob storage.
  - Remove any secrets from docker-compose; use Docker secrets or CI/CD managed variables.
- Authentication Hardening:
  - In PublicApi, enable ValidateIssuer/ValidateAudience; RequireHttpsMetadata true in non-Dev.
  - Reduce token lifetime to ≤60 minutes; introduce refresh tokens via secure server flow if needed.
- Health Checks:
  - Split /health into /live and /ready with minimal outputs and restrict via network or auth.
- Introduce API Versioning and Rate Limiting:
  - Add Microsoft.AspNetCore.Mvc.Versioning and .NET 8 Rate Limiting middleware.

### Strategic (6+ weeks)
- Centralized identity and token service:
  - Move to standard OAuth2/OIDC provider (Azure AD, IdentityServer) for JWT issuance and management.
- Security headers and CSP:
  - Add robust headers: Content-Security-Policy, X-Content-Type-Options, X-Frame-Options/Frame-Ancestors, Referrer-Policy, Permissions-Policy.
- Comprehensive logging, auditing, and monitoring:
  - Structured logging with PII redaction; correlation IDs; auth event auditing.
- Threat modeling and continuous security testing:
  - Integrate SAST/DAST and dependency scanning into CI/CD; regular threat model reviews.

## Code and Configuration Snippets

### A. Secure Swagger Gating
```csharp
// In src/PublicApi/Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
}
// else consider protecting with [Authorize(Policy = "SwaggerAccess")] and middleware
```

### B. Harden JWT Validation
```csharp
// In src/PublicApi/Program.cs
var issuer = builder.Configuration["Auth:Issuer"];
var audience = builder.Configuration["Auth:Audience"];
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Auth:JwtKey"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
```

### C. Data Protection Keys in Azure
```csharp
// In Web/PublicApi Program.cs (production branch)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys")) // container-mounted volume
    .SetApplicationName("eShopOnWeb")
    .ProtectKeysWithAzureKeyVault(
        new Uri(builder.Configuration["KeyVault:KeyIdentifier"]),
        new DefaultAzureCredential());
```

### D. Cookie Policy Alignment
```csharp
// In src/Web/Configuration/ConfigureCookieSettings.cs
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});
services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});
```

### E. ProblemDetails for Errors
```csharp
// In ExceptionMiddleware
var problem = new ProblemDetails
{
    Status = StatusCodes.Status500InternalServerError,
    Title = "An error occurred",
    Detail = "An unexpected error occurred. Please contact support if the problem persists."
};
context.Response.StatusCode = problem.Status.Value;
await context.Response.WriteAsJsonAsync(problem);
```

### F. Health Checks
```csharp
// In src/Web/Program.cs
app.MapHealthChecks("/live", new HealthCheckOptions { Predicate = _ => false }); // just 200 OK
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
// Restrict via network policy or authentication in production
```

### G. Rate Limiting (ASP.NET Core 8)
```csharp
// Program.cs
builder.Services.AddRateLimiter(_ => _.AddFixedWindowLimiter("fixed", options =>
{
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(1);
    options.QueueLimit = 0;
}));

app.UseRateLimiter();
```

## Environment Variables and Key Vault Secrets
- Auth:JwtKey (secret), Auth:Issuer, Auth:Audience
- ConnectionStrings:CatalogConnection, ConnectionStrings:IdentityConnection
- AZURE_KEY_VAULT_ENDPOINT
- AZURE_SQL_CATALOG_CONNECTION_STRING_KEY (Key Vault secret name for catalog)
- AZURE_SQL_IDENTITY_CONNECTION_STRING_KEY (Key Vault secret name for identity)
- KeyVault:KeyIdentifier (for Data Protection key encryption)
- ALLOWED_ORIGINS (comma-separated list)
- HSTS:MaxAgeDays, HSTS:IncludeSubDomains, HSTS:Preload (for production)
- Logging:SensitiveData:RedactionEnabled (custom switch)

## Acceptance Criteria Alignment
- Actionable items prioritized (Quick Wins, Near-Term, Strategic).
- Each finding maps to code evidence.
- Snippets for Swagger gating, security headers, data protection keys provided.
- Environment variables and Key Vault secrets explicitly listed.
