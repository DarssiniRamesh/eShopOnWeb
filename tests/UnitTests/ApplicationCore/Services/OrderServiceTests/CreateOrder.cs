using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.OrderServiceTests;

public class CreateOrder
{
    [Fact]
    public async Task ThrowsWhenBasketNotFound()
    {
        var orderRepo = Substitute.For<IRepository<Order>>();
        var basketRepo = Substitute.For<IRepository<Basket>>();
        var itemRepo = Substitute.For<IRepository<CatalogItem>>();
        var composer = Substitute.For<IUriComposer>();

        basketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), default).Returns((Basket?)null);
        var svc = new OrderService(basketRepo, itemRepo, orderRepo, composer);

        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.CreateOrderAsync(999, new Address("a", "b", "c", "d", "e")));
    }

    [Fact]
    public async Task ThrowsWhenBasketEmpty()
    {
        var orderRepo = Substitute.For<IRepository<Order>>();
        var basketRepo = Substitute.For<IRepository<Basket>>();
        var itemRepo = Substitute.For<IRepository<CatalogItem>>();
        var composer = Substitute.For<IUriComposer>();

        var basket = new Basket("buyer"); // empty items
        basketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), default).Returns(basket);

        var svc = new OrderService(basketRepo, itemRepo, orderRepo, composer);

        await Assert.ThrowsAsync<EmptyBasketOnCheckoutException>(() =>
            svc.CreateOrderAsync(1, new Address("a", "b", "c", "d", "e")));
    }

    [Fact]
    public async Task CreatesOrderWithSnapshotAndComposedUris()
    {
        var orderRepo = Substitute.For<IRepository<Order>>();
        var basketRepo = Substitute.For<IRepository<Basket>>();
        var itemRepo = Substitute.For<IRepository<CatalogItem>>();
        var composer = Substitute.For<IUriComposer>();

        var basket = new Basket("buyer");
        basket.AddItem(1, 10m, 2);
        basket.AddItem(2, 5m, 1);

        basketRepo.FirstOrDefaultAsync(Arg.Any<BasketWithItemsSpecification>(), default).Returns(basket);

        var catalogItems = new List<CatalogItem>
        {
            new CatalogItem(1, 1, "desc1", "name1", 10m, "http://catalogbaseurltobereplaced/pic1.png"){ Id = 1 },
            new CatalogItem(2, 2, "desc2", "name2", 5m, "http://catalogbaseurltobereplaced/pic2.png"){ Id = 2 }
        };

        itemRepo.ListAsync(Arg.Any<CatalogItemsSpecification>(), default).Returns(catalogItems);
        composer.ComposePicUri(Arg.Any<string>()).Returns(ci => ((string)ci[0]).Replace("http://catalogbaseurltobereplaced", "http://base"));

        Order? addedOrder = null;
        orderRepo.AddAsync(Arg.Do<Order>(o => addedOrder = o), default).Returns(Task.CompletedTask);

        var svc = new OrderService(basketRepo, itemRepo, orderRepo, composer);

        await svc.CreateOrderAsync(1, new Address("a", "b", "c", "d", "e"));

        Assert.NotNull(addedOrder);
        Assert.Equal("buyer", addedOrder!.BuyerId);
        Assert.Equal(2, addedOrder.OrderItems.Count);
        Assert.All(addedOrder.OrderItems, i => Assert.StartsWith("http://base", i.ItemOrdered.PictureUri));
        Assert.Contains(addedOrder.OrderItems, i => i.UnitPrice == 10m && i.Units == 2);
        Assert.Contains(addedOrder.OrderItems, i => i.UnitPrice == 5m && i.Units == 1);
    }
}
