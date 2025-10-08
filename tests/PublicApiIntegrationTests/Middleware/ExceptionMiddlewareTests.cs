using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorShared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.PublicApi.Middleware;
using Xunit;

namespace PublicApiIntegrationTests.Middleware;

public class ExceptionMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExceptionMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.Configure(app =>
            {
                app.UseMiddleware<ExceptionMiddleware>();
                app.Run(context =>
                {
                    var path = context.Request.Path.Value ?? "";
                    if (path.Contains("dup"))
                        throw new DuplicateException("duplicate");
                    throw new System.Exception("boom");
                });
            });
        });
    }

    [Fact]
    public async Task ReturnsConflictOnDuplicate()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/dup");
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("duplicate", body);
    }

    [Fact]
    public async Task ReturnsInternalServerErrorOnGeneric()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/generic");
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("boom", body);
    }
}
