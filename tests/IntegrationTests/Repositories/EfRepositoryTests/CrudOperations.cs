using System.Threading.Tasks;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Data;
using Microsoft.eShopWeb.Infrastructure.Data.Config;
using Xunit;

namespace Microsoft.eShopWeb.IntegrationTests.Repositories.EfRepositoryTests
{
    public class CrudOperations
    {
        private CatalogContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CatalogContext>()
                .UseInMemoryDatabase(databaseName: $"eshop-{System.Guid.NewGuid()}")
                .Options;

            var db = new CatalogContext(options);
            db.Database.EnsureCreated();
            return db;
        }

        private IRepository<CatalogItem> GetRepository(CatalogContext ctx) => new Microsoft.eShopWeb.Infrastructure.Data.EfRepository<CatalogItem>(ctx);

        [Fact]
        public async Task AddUpdateDelete_Works_ForCatalogItem()
        {
            using var ctx = CreateContext();
            var repo = GetRepository(ctx);

            var item = new CatalogItem(catalogTypeId: 1, catalogBrandId: 1, description: "desc", name: "name", price: 9.99m, pictureUri: "pic");
            await repo.AddAsync(item);
            await ctx.SaveChangesAsync();

            var loaded = await repo.GetByIdAsync(item.Id);
            Assert.NotNull(loaded);

            loaded!.UpdateDetails(new CatalogItem.CatalogItemDetails("new", "newdesc", 12.5m));
            await repo.UpdateAsync(loaded);
            await ctx.SaveChangesAsync();

            var updated = await repo.GetByIdAsync(item.Id);
            Assert.Equal("new", updated!.Name);
            Assert.Equal(12.5m, updated.Price);

            await repo.DeleteAsync(updated);
            await ctx.SaveChangesAsync();

            var deleted = await repo.GetByIdAsync(item.Id);
            Assert.Null(deleted);
        }

        private class NameContainsSpec : Specification<CatalogItem>
        {
            public NameContainsSpec(string namePart)
            {
                Query.Where(ci => EF.Functions.Like(ci.Name, $"%{namePart}%"));
            }
        }

        [Fact]
        public async Task CountAsync_WithSpecification_FiltersCorrectly()
        {
            using var ctx = CreateContext();
            var repo = GetRepository(ctx);

            await repo.AddAsync(new CatalogItem(1, 1, "desc1", "abc", 1m, "pic"));
            await repo.AddAsync(new CatalogItem(1, 1, "desc2", "abcd", 2m, "pic"));
            await repo.AddAsync(new CatalogItem(1, 1, "desc3", "zzz", 3m, "pic"));
            await ctx.SaveChangesAsync();

            var countAbc = await repo.CountAsync(new NameContainsSpec("abc"));
            var countZ = await repo.CountAsync(new NameContainsSpec("z"));

            Assert.Equal(2, countAbc);
            Assert.Equal(1, countZ);
        }
    }
}
