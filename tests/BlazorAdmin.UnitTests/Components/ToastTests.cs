using System.Reflection;
using BlazorAdmin.Services;
using BlazorAdmin.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAdmin.UnitTests.Components;

public class ToastTests
{
    [Fact]
    public void ToastComponent_Shows_Info_SetsCssAndText()
    {
        using var ctx = TestContextFactory.Create();
        ctx.AddTestAuthorization(); // Ensure auth services available

        var svc = ctx.Services.GetRequiredService<ToastService>();
        var cut = ctx.RenderComponent<Toast>();

        svc.ShowToast("Hello Info", ToastLevel.Info);

        cut.MarkupMatches(cut.Markup); // ensure render
        var html = cut.Markup;
        Assert.Contains("toast-visible", html);
        Assert.Contains("bg-info", html);
        Assert.Contains("Info", html);
        Assert.Contains("Hello Info", html);
        Assert.Contains("fa fa-info", html);
    }

    [Fact]
    public void ToastComponent_Shows_Success_Warning_Error()
    {
        using var ctx = TestContextFactory.Create();
        var svc = ctx.Services.GetRequiredService<ToastService>();
        var cut = ctx.RenderComponent<Toast>();

        // Success
        svc.ShowToast("ok", ToastLevel.Success);
        var html = cut.Markup;
        Assert.Contains("bg-success", html);
        Assert.Contains("Success", html);
        Assert.Contains("fa fa-check", html);

        // Warning
        svc.ShowToast("warn", ToastLevel.Warning);
        html = cut.Markup;
        Assert.Contains("bg-warning", html);
        Assert.Contains("Warning", html);
        Assert.Contains("fa fa-exclamation", html);

        // Error
        svc.ShowToast("err", ToastLevel.Error);
        html = cut.Markup;
        Assert.Contains("bg-danger", html);
        Assert.Contains("Error", html);
        Assert.Contains("fa fa-times", html);
    }

    [Fact]
    public void ToastComponent_HideToast_InvokedViaService_PrivateMethod()
    {
        using var ctx = TestContextFactory.Create();
        var svc = ctx.Services.GetRequiredService<ToastService>();
        var cut = ctx.RenderComponent<Toast>();

        svc.ShowToast("temp", ToastLevel.Info);
        Assert.Contains("toast-visible", cut.Markup);

        // Simulate auto-dismiss by invoking ToastService.HideToast via reflection
        var hideMethod = typeof(ToastService).GetMethod("HideToast", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(hideMethod);

        hideMethod!.Invoke(svc, new object?[] { null, null });

        // Component should hide after event
        Assert.DoesNotContain("toast-visible", cut.Markup);
    }

    [Fact]
    public void ToastService_OnShow_StartsTimer_And_OnHide_Fires()
    {
        using var ctx = TestContextFactory.Create();
        var svc = ctx.Services.GetRequiredService<ToastService>();
        string? receivedMessage = null;
        ToastLevel? receivedLevel = null;
        bool hideCalled = false;

        svc.OnShow += (msg, level) =>
        {
            receivedMessage = msg;
            receivedLevel = level;
        };
        svc.OnHide += () => hideCalled = true;

        svc.ShowToast("ping", ToastLevel.Warning);

        Assert.Equal("ping", receivedMessage);
        Assert.Equal(ToastLevel.Warning, receivedLevel);

        // Verify internal timer created and enabled
        var timerField = typeof(ToastService).GetField("Countdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(timerField);
        var timer = timerField!.GetValue(svc) as System.Timers.Timer;
        Assert.NotNull(timer);
        Assert.True(timer!.Enabled);

        // Trigger hide event via private method
        var hideMethod = typeof(ToastService).GetMethod("HideToast", BindingFlags.NonPublic | BindingFlags.Instance);
        hideMethod!.Invoke(svc, new object?[] { null, null });

        Assert.True(hideCalled);
    }
}
