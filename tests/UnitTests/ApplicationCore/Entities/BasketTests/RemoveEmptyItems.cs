using System.Linq;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Entities.BasketTests;

public class RemoveEmptyItems
{
    private readonly string _buyerId = "buyer";

    [Fact]
    public void RemovesItemsWithZeroQuantity()
    {
        var basket = new Basket(_buyerId);
        basket.AddItem(1, 1.0m, 2);
        basket.AddItem(2, 2.0m, 0); // allowed by AddItem guard? quantity==0 throws; so set to 1 then 0
        // Since AddItem guards against negative and accepts 0..int.MaxValue we pass 0 above.
        // Explicitly set to 0 as well to simulate update path
        var item2 = basket.Items.Single(i => i.CatalogItemId == 2);
        item2.SetQuantity(0);

        basket.RemoveEmptyItems();

        Assert.DoesNotContain(basket.Items, i => i.CatalogItemId == 2);
        Assert.Single(basket.Items);
    }

    [Fact]
    public void KeepsItemsWithPositiveQuantity()
    {
        var basket = new Basket(_buyerId);
        basket.AddItem(1, 5.0m, 3);

        basket.RemoveEmptyItems();

        Assert.Single(basket.Items);
        Assert.Equal(3, basket.Items.Single().Quantity);
    }
}
