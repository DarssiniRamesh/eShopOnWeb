using System;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.Web.ViewModels;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class OrderViewModelsTests
    {
        [Fact]
        public void OrderItemViewModel_Discount_IsAlwaysZero()
        {
            var vm = new OrderItemViewModel { UnitPrice = 12m, Units = 2 };

            Assert.Equal(0m, vm.Discount);
        }

        [Fact]
        public void OrderViewModel_Status_DefaultsToPending()
        {
            var vm = new OrderViewModel
            {
                OrderNumber = 123,
                OrderDate = DateTimeOffset.UtcNow,
                Total = 42m,
                ShippingAddress = new Address("1 Main", "City", "ST", "US", "12345")
            };

            Assert.Equal("Pending", vm.Status);
        }
    }
}
