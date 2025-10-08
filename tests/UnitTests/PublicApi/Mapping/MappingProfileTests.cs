using AutoMapper;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.PublicApi;
using Microsoft.eShopWeb.PublicApi.CatalogBrandEndpoints;
using Microsoft.eShopWeb.PublicApi.CatalogItemEndpoints;
using Microsoft.eShopWeb.PublicApi.CatalogTypeEndpoints;
using Xunit;

namespace Microsoft.eShopWeb.UnitTests.PublicApi.Mapping;

public class MappingProfileTests
{
    [Fact]
    public void AssertConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void MapsCatalogItemToDto()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var entity = new CatalogItem(1, 2, "Desc", "Name", 9.99m, "pic.png")
        {
            Id = 7
        };

        var dto = mapper.Map<CatalogItemDto>(entity);

        Assert.Equal(7, dto.Id);
        Assert.Equal("Name", dto.Name);
        Assert.Equal("pic.png", dto.PictureUri);
        Assert.Equal(9.99m, dto.Price);
    }

    [Fact]
    public void MapsBrandTypeNames()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

        var brand = new CatalogBrand("BrandX") { Id = 3 };
        var brandDto = mapper.Map<CatalogBrandDto>(brand);
        Assert.Equal(3, brandDto.Id);
        Assert.Equal("BrandX", brandDto.Name);

        var type = new CatalogType("TypeY") { Id = 4 };
        var typeDto = mapper.Map<CatalogTypeDto>(type);
        Assert.Equal(4, typeDto.Id);
        Assert.Equal("TypeY", typeDto.Name);
    }
}
