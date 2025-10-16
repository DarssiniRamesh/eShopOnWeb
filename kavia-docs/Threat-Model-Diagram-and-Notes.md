# Threat Model Diagram and Notes

## System Context
The eShopOnWeb solution includes:
- Web (MVC/Razor Pages + Server-side Blazor hosting)
- PublicApi (REST)
- BlazorAdmin (WebAssembly client)
- Infrastructure with EF Core for Catalog and Identity databases
- Health checks and Swagger endpoints
- Optional Azure Key Vault and Azure SQL in production

## Data Flows and Trust Boundaries
- User browser ↔ Web app (HTTPS)
- BlazorAdmin (WASM) ↔ PublicApi (HTTPS) with JWT
- Web ↔ PublicApi (internal, HTTPS)
- Apps ↔ Databases (SQL) over secure connections
- Apps ↔ Azure Key Vault for secrets and keys

## Diagram
```mermaid
flowchart LR
    subgraph Client["Client Browser"]
        U["User"]
        BA["BlazorAdmin (WASM)"]
    end

    subgraph WebApp["Web (MVC/Razor + Server-side Blazor)"]
        WAuth["Cookie Auth<br/>Data Protection"]
        WCtrl["Controllers & Pages"]
        WHealth["/health, /live, /ready"]
    end

    subgraph PublicAPI["PublicApi (REST)"]
        APIAuth["JWT Bearer Auth"]
        APIEndpoints["Endpoints & Controllers"]
        Swagger["Swagger UI/JSON"]
        ExMiddle["Exception Middleware"]
    end

    subgraph Infra["Infrastructure"]
        DB1["Catalog DB (SQL)"]
        DB2["Identity DB (SQL)"]
        KV["Azure Key Vault"]
        DP["Data Protection Keys"]
    end

    U -->|HTTPS| WCtrl
    BA -->|HTTPS + JWT| APIEndpoints
    WCtrl -->|HTTPS| APIEndpoints
    WCtrl -->|EF Core| DB1
    WCtrl -->|EF Core| DB2
    APIEndpoints -->|EF Core| DB1
    APIEndpoints -->|EF Core| DB2
    WAuth --> DP
    APIAuth --> DP
    WCtrl -->|Secrets| KV
    APIEndpoints -->|Secrets| KV
    APIEndpoints --> Swagger
    WCtrl --> WHealth
```

## Threats and Controls Overview
- Threat: Token exfiltration (XSS) on client side.
  - Control: Avoid token storage in Local Storage; CSP; sanitize outputs; short token lifetime.
- Threat: Secret leakage from source or container manifests.
  - Control: Key Vault, env vars, remove hardcoded secrets; rotate keys.
- Threat: API enumeration through Swagger.
  - Control: Disable in production or require auth; hide PII.
- Threat: DoS/bruteforce on endpoints.
  - Control: Rate limiting; account lockout; anti-automation.
- Threat: Information disclosure via errors and health outputs.
  - Control: ProblemDetails with generic messages; restrict health endpoints.

## Notes and Assumptions
- Production deployments will have TLS termination and HSTS enabled.
- Data Protection keys are persisted and encrypted in production.
- Admin operations via BlazorAdmin require strict origin CORS and auth.
