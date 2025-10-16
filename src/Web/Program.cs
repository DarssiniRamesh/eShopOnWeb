using System.Net.Mime;
using Ardalis.ListStartupServices;
using Azure.Identity;
using BlazorAdmin;
using BlazorAdmin.Services;
using Blazored.LocalStorage;
using BlazorShared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopWeb;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Data;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web;
using Microsoft.eShopWeb.Web.Configuration;
using Microsoft.eShopWeb.Web.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi.Models;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

// Add environment variables as default config source first
builder.Configuration.AddEnvironmentVariables();

// Azure Key Vault configuration (fallback to env vars)
var keyVaultUri = builder.Configuration["AZURE_KEYVAULT_URI"] ?? builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    try
    {
        var credential = new ChainedTokenCredential(new AzureDeveloperCliCredential(), new DefaultAzureCredential());
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
        builder.Logging.CreateLogger("Startup").LogInformation("Azure Key Vault configuration added.");
    }
    catch (Exception ex)
    {
        builder.Logging.CreateLogger("Startup").LogWarning(ex, "Failed to add Azure Key Vault configuration. Falling back to environment variables and appsettings.");
    }
}

// Database configuration
if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Docker")
{
    Microsoft.eShopWeb.Infrastructure.Dependencies.ConfigureServices(builder.Configuration, builder.Services);
}
else
{
    // Configure SQL Server (prod) - connection string names come from env/KeyVault
    var credential = new ChainedTokenCredential(new AzureDeveloperCliCredential(), new DefaultAzureCredential());
    // Key names stored in configuration
    var catalogConnKey = builder.Configuration["AZURE_SQL_CATALOG_CONNECTION_STRING_KEY"];
    var identityConnKey = builder.Configuration["AZURE_SQL_IDENTITY_CONNECTION_STRING_KEY"];

    if (!string.IsNullOrWhiteSpace(keyVaultUri))
    {
        // already added KV as a configuration provider above
    }

    builder.Services.AddDbContext<CatalogContext>(c =>
    {
        var connectionString = !string.IsNullOrWhiteSpace(catalogConnKey)
            ? builder.Configuration[catalogConnKey!]
            : builder.Configuration.GetConnectionString("CatalogConnection");
        c.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
    });
    builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    {
        var connectionString = !string.IsNullOrWhiteSpace(identityConnKey)
            ? builder.Configuration[identityConnKey!]
            : builder.Configuration.GetConnectionString("IdentityConnection");
        options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
    });
}

// Data Protection keys configuration (persist and encrypt in production)
var dpKeysPath = builder.Configuration["DP_KEYS_PATH"];
var dp = builder.Services.AddDataProtection().SetApplicationName("eShopOnWeb");
if (!string.IsNullOrWhiteSpace(dpKeysPath))
{
    Directory.CreateDirectory(dpKeysPath);
    dp.PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));
}
else if (!builder.Environment.IsDevelopment())
{
    var defaultPath = Path.Combine(AppContext.BaseDirectory, "dpkeys");
    Directory.CreateDirectory(defaultPath);
    dp.PersistKeysToFileSystem(new DirectoryInfo(defaultPath));
}
var kvKeyIdentifier = builder.Configuration["KeyVault:KeyIdentifier"];
if (!string.IsNullOrWhiteSpace(kvKeyIdentifier))
{
    dp.ProtectKeysWithAzureKeyVault(new Uri(kvKeyIdentifier), new DefaultAzureCredential());
}

builder.Services.AddCookieSettings();

// Reinforce application cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});

// Request size limits (Kestrel)
var maxBodySizeStr = builder.Configuration["MAX_REQUEST_BODY_SIZE"];
if (long.TryParse(maxBodySizeStr, out var maxBodySize) && maxBodySize > 0)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = maxBodySize;
    });
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
           .AddDefaultUI()
           .AddEntityFrameworkStores<AppIdentityDbContext>()
           .AddDefaultTokenProviders();

builder.Services.AddScoped<ITokenClaimsService, IdentityTokenClaimService>();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);

// Add memory cache services
builder.Services.AddMemoryCache();
builder.Services.AddRouting(options =>
{
    // Replace the type and the name used to refer to it with your own
    // IOutboundParameterTransformer implementation
    options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
});

builder.Services.AddMvc(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(
             new SlugifyParameterTransformer()));

});
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Basket/Checkout");
});
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddHealthChecks()
    .AddCheck<ApiHealthCheck>("api_health_check", tags: new[] { "apiHealthCheck" })
    .AddCheck<HomePageHealthCheck>("home_page_health_check", tags: new[] { "homePageHealthCheck" });
builder.Services.Configure<ServiceConfig>(config =>
{
    config.Services = new List<ServiceDescriptor>(builder.Services);
    config.Path = "/allservices";
});

// blazor configuration
var configSection = builder.Configuration.GetRequiredSection(BaseUrlConfiguration.CONFIG_NAME);
builder.Services.Configure<BaseUrlConfiguration>(configSection);
var baseUrlConfig = configSection.Get<BaseUrlConfiguration>();

// Blazor Admin Required Services for Prerendering
builder.Services.AddScoped<HttpClient>(s => new HttpClient
{
    BaseAddress = new Uri(baseUrlConfig!.WebBase)
});

// add blazor services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<HttpService>();
builder.Services.AddBlazorServices();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.Logger.LogInformation("App created...");

app.Logger.LogInformation("Seeding Database...");

using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;
    try
    {
        var catalogContext = scopedProvider.GetRequiredService<CatalogContext>();
        await CatalogContextSeed.SeedAsync(catalogContext, app.Logger);

        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scopedProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var identityContext = scopedProvider.GetRequiredService<AppIdentityDbContext>();
        await AppIdentityDbContextSeed.SeedAsync(identityContext, userManager, roleManager);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

var catalogBaseUrl = builder.Configuration.GetValue(typeof(string), "CatalogBaseUrl") as string;
if (!string.IsNullOrEmpty(catalogBaseUrl))
{
    app.Use((context, next) =>
    {
        context.Request.PathBase = new PathString(catalogBaseUrl);
        return next();
    });
}

app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            var result = new
            {
                status = report.Status.ToString(),
                errors = report.Entries.Select(e => new
                {
                    key = e.Key,
                    value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
                })
            }.ToJson();
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(result);
        }
    });
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.Logger.LogInformation("Adding Development middleware...");
    app.UseDeveloperExceptionPage();
    app.UseShowAllServicesMiddleware();
    app.UseMigrationsEndPoint();
    app.UseWebAssemblyDebugging();
}
else
{
    app.Logger.LogInformation("Adding non-Development middleware...");
    app.UseExceptionHandler("/Error");
    // Configure HSTS with stricter defaults
    var hstsMaxAgeDays = int.TryParse(builder.Configuration["HSTS:MaxAgeDays"], out var d) ? d : 365;
    var hstsIncludeSubdomains = bool.TryParse(builder.Configuration["HSTS:IncludeSubDomains"], out var inc) ? inc : true;
    var hstsPreload = bool.TryParse(builder.Configuration["HSTS:Preload"], out var preload) ? preload : true;
    app.UseHsts(hsts => {
        hsts.MaxAge = TimeSpan.FromDays(hstsMaxAgeDays);
        hsts.IncludeSubDomains = hstsIncludeSubdomains;
        hsts.Preload = hstsPreload;
    });
}

// Enforce HTTPS always
app.UseHttpsRedirection();

// Apply global security headers
app.UseSecurityHeaders();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

// Swagger gating (optional for Web - only enable if explicitly requested via env)
var enableSwagger = string.Equals(builder.Configuration["ENABLE_SWAGGER"], "true", StringComparison.OrdinalIgnoreCase);
if (enableSwagger && (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker"))
{
    // Placeholder if swagger is added to Web in future; currently PublicApi hosts Swagger
}

app.MapControllerRoute("default", "{controller:slugify=Home}/{action:slugify=Index}/{id?}");
app.MapRazorPages();

// Health checks: expose minimal info by default
app.MapHealthChecks("/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("homePageHealthCheck") || check.Tags.Contains("apiHealthCheck") });

app.MapHealthChecks("home_page_health_check", new HealthCheckOptions { Predicate = check => check.Tags.Contains("homePageHealthCheck") });
app.MapHealthChecks("api_health_check", new HealthCheckOptions { Predicate = check => check.Tags.Contains("apiHealthCheck") });
//endpoints.MapBlazorHub("/admin");
app.MapFallbackToFile("index.html");

app.Logger.LogInformation("LAUNCHING");
app.Run();
