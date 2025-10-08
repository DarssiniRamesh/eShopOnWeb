using System.Net;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.eShopWeb.Web.Features.OrderDetails;
using Xunit;

namespace Microsoft.eShopWeb.IntegrationTests.Controllers;

public class OrderControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> AuthenticatedFactoryWithMediator(object mediatorResponse)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>("Test", _ => { });

                var mediator = NSubstitute.Substitute.For<IMediator>();
                mediator.Send(Arg.Any<GetOrderDetails>(), Arg.Any<CancellationToken>())
                    .Returns(mediatorResponse);

                services.AddSingleton(mediator);
            });
        });
    }

    [Fact]
    public async Task Detail_ReturnsBadRequest_WhenOrderNotFound()
    {
        var factory = AuthenticatedFactoryWithMediator(null);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Test");

        var resp = await client.GetAsync("/Order/Detail/10");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Detail_ReturnsOk_WhenOrderExists()
    {
        var vm = new OrderDetailViewModel
        {
            OrderId = 77,
            ShippingAddress = "addr",
            OrderDate = System.DateTimeOffset.Now,
            OrderItems = new System.Collections.Generic.List<OrderItemViewModel>()
        };

        var factory = AuthenticatedFactoryWithMediator(vm);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Test");

        var resp = await client.GetAsync("/Order/Detail/77");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}

// Reuse minimal auth
public class TestAuthHandlerOptions : AuthenticationSchemeOptions { }
public class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TestAuthHandlerOptions>
{
    public TestAuthHandler(Microsoft.Extensions.Options.IOptionsMonitor<TestAuthHandlerOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "tester@eshop") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
