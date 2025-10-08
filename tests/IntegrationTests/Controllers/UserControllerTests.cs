using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.eShopWeb.IntegrationTests.Controllers;

public class UserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // No-op: we keep defaults, endpoints should return anonymous user when unauthenticated
            });
        });
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsAnonymousWhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/User");
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"isAuthenticated\":false", json.ToLowerInvariant());
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsTokenForAuthenticatedUser()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Add fake auth
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>("Test", _ => { });
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var resp = await client.GetAsync("/User");
        resp.EnsureSuccessStatusCode();
        var s = await resp.Content.ReadAsStringAsync();
        // response should include token string (may be empty, but present)
        Assert.Contains("token", s.ToLowerInvariant());
    }
}

// Minimal test auth handler
public class TestAuthHandlerOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions { }
public class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TestAuthHandlerOptions>
{
    public TestAuthHandler(Microsoft.Extensions.Options.IOptionsMonitor<TestAuthHandlerOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        Microsoft.AspNetCore.Authentication.ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "tester@eshop") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
