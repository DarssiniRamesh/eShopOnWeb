using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.BasketServiceTests;

public class SetQuantitiesTests
{
    private readonly IRepository<Basket> _mockBasketRepo = Substitute.For<IRepository<Basket>>();
    private readonly IAppLogger<BasketService> _mockLogger = Substitute.For<IAppLogger<BasketService>>();

    [Fact]
    public async Task ReturnsNotFoundWhenBasketMissing()
    {
        _mockBasketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Basket?)null);

        var service = new BasketService(_mockBasketRepo, _mockLogger);

        var result = await service.SetQuantities(123, new Dictionary<string, int>());

        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _mockBasketRepo.DidNotReceive().UpdateAsync(Arg.Any<Basket>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatesExistingItemQuantitiesAndRemovesZeros()
    {
        // Arrange existing basket with two items
        var basket = new Basket("buyer-1");
        basket.AddItem(10, 2.0m, 2);
        basket.AddItem(20, 5.0m, 3);

        // Map current item Ids to new quantities: first -> 5, second -> 0 (removal)
        var quantities = basket.Items.ToDictionary(i => i.Id.ToString(), i => i.CatalogItemId == 10 ? 5 : 0);

        _mockBasketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(basket);

        var service = new BasketService(_mockBasketRepo, _mockLogger);

        // Act
        var result = await service.SetQuantities(999, quantities);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(basket.Items);
        Assert.Equal(10, basket.Items.First().CatalogItemId);
        Assert.Equal(5, basket.Items.First().Quantity);

        await _mockBasketRepo.Received(1).UpdateAsync(basket, Arg.Any<CancellationToken>());
    }
}
