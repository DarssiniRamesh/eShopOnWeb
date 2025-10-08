using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Entities.OrderTests;

public class OrderCreateAndTotalTests
{
    [Fact]
    public void CalculatesTotalFromOrderItems()
    {
        var address = new Address("st", "ct", "st", "zip", "ctry");
        var itemOrdered = new CatalogItemOrdered(1, "name", "pic");
        var oi1 = new OrderItem(itemOrdered, 10m, 2); // 20
        var oi2 = new OrderItem(itemOrdered, 5m, 3);  // 15

        var order = new Order("buyer", address, new System.Collections.Generic.List<OrderItem> { oi1, oi2 });

        Assert.Equal(35m, order.Total());
    }
}
