using System.Threading.Tasks;
using Microsoft.eShopWeb.Web.Pages;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.eShopWeb.Web.ViewModels;
using NSubstitute;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class IndexModelTests
    {
        [Fact]
        public async Task OnGet_SetsCatalogModel_FromService()
        {
            var expected = new CatalogIndexViewModel();
            var svc = Substitute.For<ICatalogViewModelService>();
            svc.GetCatalogItems(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int?>())
               .Returns(expected);

            var page = new IndexModel(svc);

            var incoming = new CatalogIndexViewModel { BrandFilterApplied = 1, TypesFilterApplied = 2 };

            await page.OnGet(incoming, pageId: 3);

            Assert.Same(expected, page.CatalogModel);
            await svc.Received(1).GetCatalogItems(3, Arg.Any<int>(), 1, 2);
        }

        [Fact]
        public async Task OnGet_DefaultsPageIdToZero_WhenNull()
        {
            var svc = Substitute.For<ICatalogViewModelService>();
            svc.GetCatalogItems(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int?>())
               .Returns(new CatalogIndexViewModel());

            var page = new IndexModel(svc);

            await page.OnGet(new CatalogIndexViewModel(), null);

            await svc.Received(1).GetCatalogItems(0, Arg.Any<int>(), null, null);
        }
    }
}
