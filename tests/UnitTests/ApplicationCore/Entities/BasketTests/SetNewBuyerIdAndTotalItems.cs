using System.Linq;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Entities.BasketTests;

public class SetNewBuyerIdAndTotalItems
{
    [Fact]
    public void SetNewBuyerId_UpdatesBuyer()
    {
        var basket = new Basket("oldBuyer");
        basket.SetNewBuyerId("newBuyer");

        Assert.Equal("newBuyer", basket.BuyerId);
    }

    [Fact]
    public void TotalItems_SumsQuantities()
    {
        var basket = new Basket("buyer");
        basket.AddItem(1, 1m, 2);
        basket.AddItem(2, 2m, 3);

        Assert.Equal(5, basket.TotalItems);
    }

    [Fact]
    public void TotalItems_ReflectsIncrementOnDuplicateAdd()
    {
        var basket = new Basket("buyer");
        basket.AddItem(1, 1m, 2);
        basket.AddItem(1, 1m, 4);

        Assert.Equal(6, basket.TotalItems);
        Assert.Single(basket.Items);
        Assert.Equal(6, basket.Items.Single().Quantity);
    }
}
