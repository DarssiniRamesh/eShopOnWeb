using System.Linq;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Entities.BasketTests;

public class BasketSetQuantitiesAndRemoveTests
{
    [Fact]
    public void RemoveEmptyItemsRemovesZerosAndNegatives()
    {
        var basket = new Basket("buyer");
        basket.AddItem(1, 1m, 0);
        basket.AddItem(2, 1m, -1);
        basket.AddItem(3, 1m, 2);

        basket.RemoveEmptyItems();

        Assert.Single(basket.Items);
        Assert.Equal(3, basket.Items.First().CatalogItemId);
    }

    [Fact]
    public void AddItemMergesSameCatalogItem()
    {
        var basket = new Basket("buyer");
        basket.AddItem(5, 2m, 1);
        basket.AddItem(5, 2m, 3);

        Assert.Single(basket.Items);
        Assert.Equal(4, basket.Items.First().Quantity);
    }
}
