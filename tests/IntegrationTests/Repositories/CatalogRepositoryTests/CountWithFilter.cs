using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.eShopWeb.Infrastructure.Data;
using Xunit;

namespace Microsoft.eShopWeb.IntegrationTests.Repositories.CatalogRepositoryTests;

public class CountWithFilter
{
    private CatalogContext NewContext()
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseInMemoryDatabase(databaseName: $"catalog-{System.Guid.NewGuid()}")
            .Options;
        return new CatalogContext(options);
    }

    [Fact]
    public async Task CountsByBrandAndType()
    {
        using var ctx = NewContext();

        ctx.CatalogBrands.Add(new CatalogBrand("b1") { Id = 1 });
        ctx.CatalogBrands.Add(new CatalogBrand("b2") { Id = 2 });
        ctx.CatalogTypes.Add(new CatalogType("t1") { Id = 1 });
        ctx.CatalogTypes.Add(new CatalogType("t2") { Id = 2 });

        ctx.CatalogItems.Add(new CatalogItem(1, 1, "d", "n", 1m, "p") { Id = 1 });
        ctx.CatalogItems.Add(new CatalogItem(1, 2, "d", "n", 1m, "p") { Id = 2 });
        ctx.CatalogItems.Add(new CatalogItem(2, 1, "d", "n", 1m, "p") { Id = 3 });
        await ctx.SaveChangesAsync();

        var repo = new EfRepository<CatalogItem>(ctx);
        var spec = new CatalogFilterSpecification(brandId: 1, typeId: 1);

        var count = await repo.CountAsync(spec);

        Assert.Equal(1, count);
    }
}
