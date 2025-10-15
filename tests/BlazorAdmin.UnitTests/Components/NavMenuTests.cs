using BlazorAdmin.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorAdmin.UnitTests.Components;

public class NavMenuTests
{
    [Fact]
    public void NavMenu_Toggle_Collapse_Class_Changes_On_Click()
    {
        using var ctx = TestContextFactory.Create();
        ctx.AddTestAuthorization();

        var cut = ctx.RenderComponent<NavMenu>();

        // initial collapsed
        Assert.Contains("collapse", cut.Markup);

        // click toggle button
        cut.Find("button.navbar-toggler").Click();

        // after toggle: collapse class should be absent on the menu container
        Assert.DoesNotContain("collapse", cut.Markup);
    }

    [Fact]
    public void NavMenu_Authorized_Shows_UserLinks_And_Active_Link_On_Admin()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("alice");
        auth.SetRoles("Administrators");

        // Navigate to /admin before render so NavLink becomes active
        var nav = ctx.Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        nav!.NavigateTo("http://localhost/admin");

        var cut = ctx.RenderComponent<NavMenu>();

        // Should show Logout link and account link
        Assert.Contains("Logout", cut.Markup);
        Assert.Contains("manage/my-account", cut.Markup);

        // Admin link should be active
        var adminLink = cut.Find("a.nav-link[href=\"admin\"]");
        Assert.Contains("active", adminLink.ClassList);
    }

    [Fact]
    public void NavMenu_Unauthorized_DoesNot_Show_Protected_Links()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();
        auth.SetNotAuthorized();

        var cut = ctx.RenderComponent<NavMenu>();

        Assert.DoesNotContain("manage/my-account", cut.Markup);
        Assert.DoesNotContain("Logout", cut.Markup);
    }
}
