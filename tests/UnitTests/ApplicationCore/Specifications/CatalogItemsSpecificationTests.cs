using System.Collections.Generic;
using System.Linq;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.ApplicationCore.Specifications;

public class CatalogItemsSpecificationTests
{
    [Fact]
    public void FiltersByIds()
    {
        var items = new List<CatalogItem>
        {
            new CatalogItem(1,1,1,"A","",1m,""),
            new CatalogItem(2,1,1,"B","",1m,""),
            new CatalogItem(3,1,1,"C","",1m,"")
        }.AsQueryable();

        var spec = new CatalogItemsSpecification(new[] { 1, 3 });
        var result = items.Where(spec.WhereExpressions.First().Filter.Compile()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id == 1);
        Assert.Contains(result, i => i.Id == 3);
    }
}
