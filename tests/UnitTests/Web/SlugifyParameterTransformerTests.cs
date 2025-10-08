using System;
using Xunit;
using Microsoft.eShopWeb.Web;

namespace Microsoft.eShopWeb.UnitTests.Web
{
    public class SlugifyParameterTransformerTests
    {
        [Fact]
        public void TransformOutbound_ReturnsNull_ForNullValue()
        {
            var transformer = new SlugifyParameterTransformer();

            var result = transformer.TransformOutbound(null);

            Assert.Null(result);
        }

        [Fact]
        public void TransformOutbound_ReturnsNull_ForEmptyString()
        {
            var transformer = new SlugifyParameterTransformer();

            var result = transformer.TransformOutbound(string.Empty);

            Assert.Null(result);
        }

        [Fact]
        public void TransformOutbound_SlugifiesPascalCase()
        {
            var transformer = new SlugifyParameterTransformer();

            var result = transformer.TransformOutbound("OrderDetailsPage");

            Assert.Equal("order-details-page", result);
        }

        [Fact]
        public void TransformOutbound_SlugifiesCamelCase()
        {
            var transformer = new SlugifyParameterTransformer();

            var result = transformer.TransformOutbound("orderDetailsPage");

            Assert.Equal("order-details-page", result);
        }

        [Fact]
        public void TransformOutbound_LeavesAlreadySlugified_Lowercase()
        {
            var transformer = new SlugifyParameterTransformer();

            var result = transformer.TransformOutbound("order-details-page");

            Assert.Equal("order-details-page", result);
        }

        [Fact]
        public void TransformOutbound_LeavesNumbersAndHyphens()
        {
            var transformer = new SlugifyParameterTransformer();

            var result = transformer.TransformOutbound("Product123Details");

            Assert.Equal("product123-details", result);
        }
    }
}
