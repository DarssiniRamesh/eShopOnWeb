using System.Collections.Generic;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Extensions.GuardExtensionsTests;

public class EmptyBasketOnCheckoutTests
{
    [Fact]
    public void ThrowsForEmptyBasket()
    {
        var empty = new List<BasketItem>().AsReadOnly();

        Assert.Throws<EmptyBasketOnCheckoutException>(() => Guard.Against.EmptyBasketOnCheckout(empty));
    }

    [Fact]
    public void DoesNotThrowForNonEmptyBasket()
    {
        var item = new BasketItem(catalogItemId: 1, quantity: 1, unitPrice: 10.0m);
        var items = new List<BasketItem> { item }.AsReadOnly();

        var ex = Record.Exception(() => Guard.Against.EmptyBasketOnCheckout(items));

        Assert.Null(ex);
    }
}
