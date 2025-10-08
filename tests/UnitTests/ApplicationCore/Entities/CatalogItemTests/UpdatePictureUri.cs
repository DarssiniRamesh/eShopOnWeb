using Microsoft.eShopWeb.ApplicationCore.Entities;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Entities.CatalogItemTests
{
    public class UpdatePictureUri
    {
        [Fact]
        public void SetsEmpty_WhenPictureNameIsNullOrEmpty()
        {
            var item = new CatalogItem(1, 1, "desc", "name", 1m, "pic");

            item.UpdatePictureUri("");
            Assert.Equal(string.Empty, item.PictureUri);

            item.UpdatePictureUri(null!);
            Assert.Equal(string.Empty, item.PictureUri);
        }

        [Fact]
        public void SetsUri_WhenPictureNameProvided()
        {
            var item = new CatalogItem(1, 1, "desc", "name", 1m, "pic");

            item.UpdatePictureUri("image.png");

            Assert.StartsWith(@"images\products\image.png?", item.PictureUri);
        }
    }
}
