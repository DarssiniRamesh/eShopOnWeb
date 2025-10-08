using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.Result;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.BasketServiceTests;

public class SetQuantities
{
    [Fact]
    public async Task RemovesItemsWithZeroQuantityAndReturnsBasket()
    {
        var repo = Substitute.For<IRepository<Basket>>();
        var logger = Substitute.For<IAppLogger<BasketService>>();

        var basket = new Basket("buyer");
        basket.AddItem(1, 1m, 2);
        basket.AddItem(2, 2m, 3);

        // Assign unique IDs to items via reflection (BaseEntity.Id has protected setter)
        var item1 = basket.Items.First(i => i.CatalogItemId == 1);
        var item2 = basket.Items.First(i => i.CatalogItemId == 2);
        var idProp = typeof(Microsoft.eShopWeb.ApplicationCore.Entities.BaseEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp!.SetValue(item1, 11);
        idProp!.SetValue(item2, 22);

        repo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), default).Returns(basket);

        var svc = new BasketService(repo, logger);

        var updates = new Dictionary<string, int>
        {
            { item1.Id.ToString(), 0 },
            { item2.Id.ToString(), 5 }
        };

        var result = await svc.SetQuantities(basket.Id, updates);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Contains(result.Value.Items, i => i.CatalogItemId == 2 && i.Quantity == 5);
        await repo.Received().UpdateAsync(basket, default);
        logger.Received().LogInformation(Arg.Is<string>(s => s.Contains("Updating quantity")));
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBasketMissing()
    {
        var repo = Substitute.For<IRepository<Basket>>();
        var logger = Substitute.For<IAppLogger<BasketService>>();

        repo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), default).Returns((Basket?)null);

        var svc = new BasketService(repo, logger);

        var result = await svc.SetQuantities(123, new Dictionary<string, int>());

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
