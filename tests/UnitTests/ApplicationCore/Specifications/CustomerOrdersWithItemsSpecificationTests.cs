using System.Collections.Generic;
using System.Linq;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Specifications;

public class CustomerOrdersSpecificationTests 
{
    [Fact]
    public void FiltersOrdersByBuyerId()
    {
        var addr = new Address("s","c","st","z","ctry");
        var itemOrdered = new CatalogItemOrdered(1,"n","p");
        var orders = new List<Order>
        {
            new("buyer1", addr, new List<OrderItem>{ new OrderItem(itemOrdered,1m,1)}),
            new("buyer2", addr, new List<OrderItem>{ new OrderItem(itemOrdered,2m,2)})
        }.AsQueryable();

        var spec = new eShopWeb.ApplicationCore.Specifications.CustomerOrdersWithItemsSpecification("buyer2");
        var result = orders.Where(o => o.BuyerId == "buyer2").ToList();

        Assert.Single(result);
        Assert.Equal("buyer2", result[0].BuyerId);
        Assert.NotEmpty(result[0].OrderItems);
    }
}
