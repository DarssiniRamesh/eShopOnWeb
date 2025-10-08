using System.Collections.Generic;
using System.Linq;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Specifications;

public class BasketWithItemsSpecificationTests
{
    [Fact]
    public void MatchesByBuyerId()
    {
        var baskets = new List<Basket> { new("buyer1"), new("buyer2") }.AsQueryable();

        var spec = new BasketWithItemsSpecification("buyer2");

        var predicate = spec.WhereExpressions.First().Filter.Compile();
        var result = baskets.Where(predicate).ToList();

        Assert.Single(result);
        Assert.Equal("buyer2", result[0].BuyerId);
    }

    [Fact]
    public void MatchesByBasketId()
    {
        var b1 = new Basket("buyer1");
        var b2 = new Basket("buyer2");
        typeof(Basket).GetProperty("Id")!.SetValue(b2, 10);

        var baskets = new List<Basket> { b1, b2 }.AsQueryable();

        var spec = new BasketWithItemsSpecification(10);

        var predicate = spec.WhereExpressions.First().Filter.Compile();
        var result = baskets.Where(predicate).ToList();

        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
    }
}
