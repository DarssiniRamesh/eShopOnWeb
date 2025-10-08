using Microsoft.eShopWeb.Web.Pages.Basket;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class BasketViewModelTests
    {
        [Fact]
        public void Total_ComputesSumOfItems_RoundedToTwoDecimals()
        {
            var vm = new BasketViewModel();
            vm.Items.Add(new BasketItemViewModel { UnitPrice = 10.123m, Quantity = 2 });
            vm.Items.Add(new BasketItemViewModel { UnitPrice = 1.999m, Quantity = 1 });

            var total = vm.Total();

            Assert.Equal(22.25m, total); // 10.123*2 + 1.999 = 22.245 => rounded to 22.25
        }

        [Fact]
        public void Total_Zero_WhenNoItems()
        {
            var vm = new BasketViewModel();

            var total = vm.Total();

            Assert.Equal(0m, total);
        }
        private class BasketItemViewModel : Microsoft.eShopWeb.Web.Pages.Basket.BasketItemViewModel { }
    }
}
