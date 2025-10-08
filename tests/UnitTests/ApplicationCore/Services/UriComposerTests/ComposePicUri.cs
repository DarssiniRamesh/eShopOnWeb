using Microsoft.eShopWeb.ApplicationCore.Services;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Services.UriComposerTests;

public class ComposePicUri
{
    [Theory]
    [InlineData("http://catalogbaseurltobereplaced/images/pic.png", "http://mybase", "http://mybase/images/pic.png")]
    [InlineData("http://catalogbaseurltobereplaced/pic.png", "https://cdn", "https://cdn/pic.png")]
    public void ReplacesBasePlaceholder(string input, string baseUrl, string expected)
    {
        var sut = new UriComposer(new CatalogSettings { CatalogBaseUrl = baseUrl });

        var result = sut.ComposePicUri(input);

        Assert.Equal(expected, result);
    }
}
