using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.eShopWeb.Web.Pages;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class ErrorModelTests
    {
        [Fact]
        public void OnGet_SetsRequestId_FromHttpContext_WhenNoActivity()
        {
            var model = new ErrorModel();

            // Build PageContext with HttpContext so the PageModel has a valid HttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-123";
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
            model.PageContext = new PageContext(actionContext);

            // Ensure Activity.Current is null
            Activity.Current = null;

            model.OnGet();

            Assert.Equal("trace-123", model.RequestId);
            Assert.True(model.ShowRequestId);
        }

        [Fact]
        public void OnGet_UsesActivityId_WhenAvailable()
        {
            var model = new ErrorModel();

            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "ignored-trace";
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
            model.PageContext = new PageContext(actionContext);

            var activity = new Activity("test");
            activity.Start();
            try
            {
                model.OnGet();
                Assert.Equal(activity.Id, model.RequestId);
                Assert.True(model.ShowRequestId);
            }
            finally
            {
                activity.Stop();
                Activity.Current = null;
            }
        }
    }
}
