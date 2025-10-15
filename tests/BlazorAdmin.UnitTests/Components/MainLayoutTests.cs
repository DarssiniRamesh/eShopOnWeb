using BlazorAdmin.Shared;

namespace BlazorAdmin.UnitTests.Components;

public class MainLayoutTests
{
    [Fact]
    public void MainLayout_Renders_Body_And_Toast()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Administrators");

        var cut = ctx.RenderComponent<MainLayout>(ps => ps.Add(p => p.Body, builder =>
        {
            builder.AddMarkupContent(0, "<p id='body-content'>Hello Body</p>");
        }));

        // Body content present
        cut.Find("#body-content");

        // Toast component included
        cut.FindComponent<Toast>();
    }

    [Fact]
    public void MainLayout_Shows_NavMenu_For_Admin_Role_Only()
    {
        using var ctx = TestContextFactory.Create();
        var auth = ctx.AddTestAuthorization();

        // Authorized but not admin: no NavMenu in sidebar
        auth.SetAuthorized("user");
        var cut = ctx.RenderComponent<MainLayout>(ps => ps.Add(p => p.Body, b => b.AddMarkupContent(0, "<div/>")));
        Assert.Empty(cut.FindComponents<NavMenu>());

        // With Administrators role: NavMenu present
        cut.Dispose();
        auth.SetAuthorized("admin");
        auth.SetRoles("Administrators");
        cut = ctx.RenderComponent<MainLayout>(ps => ps.Add(p => p.Body, b => b.AddMarkupContent(0, "<div/>")));
        Assert.Single(cut.FindComponents<NavMenu>());
    }
}
