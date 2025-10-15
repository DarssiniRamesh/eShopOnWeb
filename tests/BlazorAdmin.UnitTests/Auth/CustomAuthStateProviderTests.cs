using System.Net.Http.Headers;
using System.Reflection;
using BlazorAdmin;
using BlazorShared.Authorization;

namespace BlazorAdmin.UnitTests.Auth;

public class CustomAuthStateProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? Responder { get; set; }
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (Responder is null) throw new InvalidOperationException("Responder not set");
            return Task.FromResult(Responder(request));
        }
    }

    private static HttpClient CreateClient(StubHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    private static UserInfo BuildAuthedUser(string token = "abc123", string name = "Alice", string role = Constants.Roles.ADMINISTRATORS)
    {
        return new UserInfo
        {
            IsAuthenticated = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            Token = token,
            Claims = new List<ClaimValue>
            {
                new ClaimValue { Type = ClaimTypes.Name, Value = name },
                new ClaimValue { Type = ClaimTypes.Role, Value = role },
            }
        };
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Authenticated_SetsClaimsAndHeader_AndCaches()
    {
        // Arrange
        var handler = new StubHandler
        {
            Responder = _ =>
            {
                var user = BuildAuthedUser();
                var json = JsonSerializer.Serialize(user);
                var msg = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
                return msg;
            }
        };
        var http = CreateClient(handler);
        var provider = new CustomAuthStateProvider(http, NullLogger<CustomAuthStateProvider>.Instance);

        // Act - first call fetches via HTTP
        var state1 = await provider.GetAuthenticationStateAsync();

        // Assert claims and header
        Assert.True(state1.User.Identity?.IsAuthenticated);
        Assert.Equal("Alice", state1.User.Identity?.Name);
        Assert.True(state1.User.IsInRole(Constants.Roles.ADMINISTRATORS));
        AuthenticationHeaderValue? authHeader = http.DefaultRequestHeaders.Authorization;
        Assert.NotNull(authHeader);
        Assert.Equal("Bearer", authHeader!.Scheme);
        Assert.Equal("abc123", authHeader.Parameter);

        int callsAfterFirst = handler.Calls;
        Assert.Equal(1, callsAfterFirst);

        // Act - second call within TTL should use cache (no new HTTP call)
        var state2 = await provider.GetAuthenticationStateAsync();

        // Assert same identity and still 1 HTTP call
        Assert.NotNull(state2.User.Identity);
        Assert.True(state2.User.Identity!.IsAuthenticated);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_CacheExpired_PerformsNewFetch()
    {
        // Arrange
        var handler = new StubHandler
        {
            Responder = _ =>
            {
                var user = BuildAuthedUser(token: Guid.NewGuid().ToString());
                var json = JsonSerializer.Serialize(user);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
        };
        var http = CreateClient(handler);
        var provider = new CustomAuthStateProvider(http, NullLogger<CustomAuthStateProvider>.Instance);

        // First call populates cache
        var _ = await provider.GetAuthenticationStateAsync();
        Assert.Equal(1, handler.Calls);

        // Force cache expiry by manipulating private field
        var field = typeof(CustomAuthStateProvider).GetField("_userLastCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(provider, DateTimeOffset.Now.AddMinutes(-10));

        // Act - next call after simulated expiry
        var state = await provider.GetAuthenticationStateAsync();

        // Assert: new HTTP call performed
        Assert.Equal(2, handler.Calls);
        Assert.True(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Unauthenticated_ReturnsAnonymous_NoHeader()
    {
        // Arrange
        var handler = new StubHandler
        {
            Responder = _ =>
            {
                var user = new UserInfo { IsAuthenticated = false, NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role, Token = null!, Claims = null! };
                var json = JsonSerializer.Serialize(user);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
        };
        var http = CreateClient(handler);
        var provider = new CustomAuthStateProvider(http, NullLogger<CustomAuthStateProvider>.Instance);

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Null(http.DefaultRequestHeaders.Authorization);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_HttpError_ReturnsAnonymous()
    {
        // Arrange
        var handler = new StubHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var http = CreateClient(handler);
        var provider = new CustomAuthStateProvider(http, NullLogger<CustomAuthStateProvider>.Instance);

        // Act
        var state = await provider.GetAuthenticationStateAsync();

        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Null(http.DefaultRequestHeaders.Authorization);
        Assert.Equal(1, handler.Calls);
    }
}
