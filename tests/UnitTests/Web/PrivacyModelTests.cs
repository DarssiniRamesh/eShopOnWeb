using Microsoft.eShopWeb.Web.Pages;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class PrivacyModelTests
    {
        [Fact]
        public void OnGet_DoesNotThrow()
        {
            var page = new PrivacyModel();

            page.OnGet();

            // No assertions needed; just ensuring no exception is thrown.
            Assert.True(true);
        }
    }
}
