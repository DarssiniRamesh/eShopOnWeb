using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorShared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.PublicApi.Middleware;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.PublicApi.Middleware;

public class ExceptionMiddlewareTests
{
    private static HttpContext NewContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task Returns409ForDuplicateException()
    {
        var context = NewContext();
        var middleware = new ExceptionMiddleware(_ => throw new DuplicateException("dup"));

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var text = await reader.ReadToEndAsync();
        var details = JsonSerializer.Deserialize<ErrorDetails>(text);
        Assert.Equal((int)HttpStatusCode.Conflict, details!.StatusCode);
        Assert.Contains("dup", details.Message);
    }

    [Fact]
    public async Task Returns500ForUnhandledException()
    {
        var context = NewContext();
        var middleware = new ExceptionMiddleware(_ => throw new Exception("boom"));

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }
}
