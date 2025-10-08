using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.eShopWeb.Web.Views.Manage;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class ManageNavPagesTests
    {
        private static ViewContext BuildViewContext(string? active)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor());
            var vc = new ViewContext
            {
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    [ManageNavPages.ActivePageKey] = active
                },
                ActionDescriptor = actionContext.ActionDescriptor,
                HttpContext = actionContext.HttpContext,
                RouteData = actionContext.RouteData
            };
            return vc;
        }

        [Fact]
        public void PageNavClass_ReturnsActive_WhenMatches()
        {
            var vc = BuildViewContext(ManageNavPages.Index);

            var result = ManageNavPages.PageNavClass(vc, ManageNavPages.Index);

            Assert.Equal("active", result);
        }

        [Fact]
        public void PageNavClass_ReturnsEmpty_WhenDoesNotMatch()
        {
            var vc = BuildViewContext(ManageNavPages.Index);

            var result = ManageNavPages.PageNavClass(vc, ManageNavPages.ChangePassword);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void AddActivePage_SetsActivePageKey()
        {
            var vc = BuildViewContext(null);

            vc.ViewData.AddActivePage(ManageNavPages.ExternalLogins);

            Assert.Equal(ManageNavPages.ExternalLogins, vc.ViewData[ManageNavPages.ActivePageKey]);
        }
    }
}
