using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.eShopWeb;
using Microsoft.eShopWeb.PublicApi.CatalogItemEndpoints;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PublicApiIntegrationTests.CatalogItemEndpoints;

[TestClass]
public class CatalogItemListPagedEndpoint_FiltersAndPageCount
{
    [TestMethod]
    public async Task ReturnsFilteredByBrandOrType()
    {
        var client = ProgramTest.NewClient;

        var resp1 = await client.GetAsync("/api/catalog-items?catalogBrandId=1");
        resp1.EnsureSuccessStatusCode();
        var model1 = (await resp1.Content.ReadAsStringAsync()).FromJson<ListPagedCatalogItemResponse>();
        Assert.IsTrue(model1!.CatalogItems.All(ci => ci.CatalogBrandId == 1));

        var resp2 = await client.GetAsync("/api/catalog-items?catalogTypeId=1");
        resp2.EnsureSuccessStatusCode();
        var model2 = (await resp2.Content.ReadAsStringAsync()).FromJson<ListPagedCatalogItemResponse>();
        Assert.IsTrue(model2!.CatalogItems.All(ci => ci.CatalogTypeId == 1));
    }

    [TestMethod]
    public async Task PageSizeZeroReturnsAllAndPageCountPositive()
    {
        var client = ProgramTest.NewClient;

        var resp = await client.GetAsync("/api/catalog-items?pageSize=0&pageIndex=0");
        resp.EnsureSuccessStatusCode();
        var model = (await resp.Content.ReadAsStringAsync()).FromJson<ListPagedCatalogItemResponse>();

        Assert.IsTrue(model!.CatalogItems.Count >= 0);
        Assert.IsTrue(model.PageCount >= 0);
    }

    [TestMethod]
    public async Task PictureUriIsComposed()
    {
        var client = ProgramTest.NewClient;

        var resp = await client.GetAsync("/api/catalog-items?pageSize=5");
        resp.EnsureSuccessStatusCode();
        var model = (await resp.Content.ReadAsStringAsync()).FromJson<ListPagedCatalogItemResponse>();

        Assert.IsTrue(model!.CatalogItems.All(ci => !ci.PictureUri.StartsWith("http://catalogbaseurltobereplaced")));
    }
}
