using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.eShopWeb.PublicApi.CatalogItemEndpoints;
using Xunit;

namespace PublicApiIntegrationTests.CatalogItemEndpoints;

public class UpdateCatalogItemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UpdateCatalogItemEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReturnsNotFoundForMissingItem()
    {
        var client = _factory.CreateClient();

        var request = new UpdateCatalogItemRequest
        {
            Id = 999999,
            CatalogBrandId = 1,
            CatalogTypeId = 1,
            Name = "Name",
            Description = "Desc",
            Price = 1m,
            PictureName = "a",
            PictureUri = "b"
        };

        var resp = await client.PutAsJsonAsync("/api/catalog-items", request);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task ReturnsBadRequestForInvalidModel()
    {
        var client = _factory.CreateClient();

        var request = new UpdateCatalogItemRequest
        {
            Id = 0, // invalid due to data annotations typically requiring > 0 and required fields
            Name = "",
            Description = "",
            CatalogBrandId = 0,
            CatalogTypeId = 0,
            Price = -1m
        };

        var resp = await client.PutAsJsonAsync("/api/catalog-items", request);

        // Minimal API model validation returns 400 automatically for invalid model
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
