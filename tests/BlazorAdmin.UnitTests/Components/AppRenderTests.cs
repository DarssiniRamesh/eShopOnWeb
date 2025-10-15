using BlazorAdmin.Shared;

namespace BlazorAdmin.UnitTests.Components;

public class AppRenderTests
{
    [Fact]
    public void App_NotAuthenticated_AdminRoute_Renders_RedirectToLogin()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();
        auth.SetNotAuthorized();

        // Navigate to /admin (protected)
        var nav = ctx.Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("http://localhost/admin");

        // JSInterop loose already set by TestContextFactory
        var cut = ctx.RenderComponent<App>();

        // Should render RedirectToLogin in NotAuthorized
        cut.FindComponent<RedirectToLogin>();
    }

    [Fact]
    public void App_Authorized_NotInRole_Shows_NotAuthorized_Message()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("user-without-role");

        var nav = ctx.Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("http://localhost/admin");

        var cut = ctx.RenderComponent<App>();

        Assert.Contains("Not Authorized", cut.Markup);
    }

    [Fact]
    public void App_Authorized_AdminRole_Renders_Admin_List_Page()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Administrators");

        var nav = ctx.Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("http://localhost/admin");

        var cut = ctx.RenderComponent<App>();

        // The /admin page has this heading
        Assert.Contains("Manage Product Catalog", cut.Markup);
    }

    [Fact]
    public void App_UnknownRoute_Renders_NotFound_Message()
    {
        using var ctx = TestContextFactory.Create();
        ctx.AddTestAuthorization();

        var nav = ctx.Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("http://localhost/nope");

        var cut = ctx.RenderComponent<App>();
        Assert.Contains("Sorry, there's nothing at this address.", cut.Markup);
    }
}
