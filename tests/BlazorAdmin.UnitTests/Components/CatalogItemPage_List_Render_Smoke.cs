using BlazorAdmin.Pages.CatalogItemPage;
using BlazorAdmin.UnitTests.Setup;

namespace BlazorAdmin.UnitTests.Components;

public sealed class CatalogItemPage_List_Render_Smoke : BaseTest
{
    [Fact]
    public void List_Page_Render_DoesNotThrow()
    {
        // Act
        var cut = Ctx.RenderComponent<List>();

        // Assert
        Assert.NotNull(cut);
        // Verify that the page title/header exists to ensure basic render
        Assert.Contains("Manage Product Catalog", cut.Markup);
    }
}
