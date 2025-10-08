using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services;

public class OrderServiceTests
{
    private readonly IRepository<Order> _orderRepo = Substitute.For<IRepository<Order>>();
    private readonly IRepository<Basket> _basketRepo = Substitute.For<IRepository<Basket>>();
    private readonly IRepository<CatalogItem> _itemRepo = Substitute.For<IRepository<CatalogItem>>();
    private readonly IUriComposer _uriComposer = Substitute.For<IUriComposer>();

    [Fact]
    public async Task CreatesOrderFromBasketItems()
    {
        // Arrange basket with items
        var basket = new Basket("buyer-123");
        basket.AddItem(1, 10m, 2);
        basket.AddItem(2, 20m, 1);

        _basketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(basket);

        // Catalog Items
        var ci1 = new CatalogItem(1, 1, "Desc", "Item 1", 10m, "pic1.png");
        var ci2 = new CatalogItem(1, 1, "Desc", "Item 2", 20m, "pic2.png");

        _itemRepo.ListAsync(Arg.Any<CatalogItemsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<CatalogItem> { ci1, ci2 });

        _uriComposer.ComposePicUri(Arg.Any<string>()).Returns(ci => (string)ci[0]!); // passthrough

        var svc = new OrderService(_basketRepo, _itemRepo, _orderRepo, _uriComposer);

        // Act
        await svc.CreateOrderAsync(42, new Address("street", "city", "state", "zip", "country"));

        // Assert order saved
        await _orderRepo.Received(1).AddAsync(
            Arg.Is<Order>(o => o.BuyerId == "buyer-123" && o.OrderItems.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ThrowsWhenBasketMissing()
    {
        _basketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Basket?)null);

        var svc = new OrderService(_basketRepo, _itemRepo, _orderRepo, _uriComposer);

        await Assert.ThrowsAsync<System.ArgumentNullException>(() =>
            svc.CreateOrderAsync(5, new Address("a", "b", "c", "d", "e")));
    }

    [Fact]
    public async Task ThrowsWhenBasketEmpty()
    {
        var basket = new Basket("buyer-xyz");
        _basketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), Arg.Any<CancellationToken>())
            .Returns(basket);

        var svc = new OrderService(_basketRepo, _itemRepo, _orderRepo, _uriComposer);

        await Assert.ThrowsAsync<Microsoft.eShopWeb.ApplicationCore.Exceptions.EmptyBasketOnCheckoutException>(() =>
            svc.CreateOrderAsync(5, new Address("a", "b", "c", "d", "e")));
    }
}
