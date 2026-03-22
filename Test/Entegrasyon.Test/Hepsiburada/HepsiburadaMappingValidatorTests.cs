using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Hepsiburada;

public class HepsiburadaMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ILogger<HepsiburadaMappingValidator>> _loggerMock = new();

    private HepsiburadaMappingValidator CreateSut() => new(
        mockContextFactory.Object,
        _loggerMock.Object);

    [Fact]
    public async Task ValidateProductMappingsAsync_Should_Return_Error_When_Product_Not_Found()
    {
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_Should_Return_Error_When_No_Category_Match()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Test",
            CategoryId = 1,
            BrandId = 1,
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Barcode = "1234567890123",
                    Images = new List<Image> { new() { StorageKey = "img.jpg" } }
                }
            }
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(new List<CategoryMarketplace>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kategorisi Hepsiburada'ya eşleştirilmemiş");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_Should_Return_Error_When_No_Brand()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Test",
            CategoryId = 1,
            BrandId = null,
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Barcode = "1234567890123",
                    Images = new List<Image> { new() { StorageKey = "img.jpg" } }
                }
            }
        };

        var categoryMatch = new CategoryMarketplace { CategoryId = 1, MarketPlaceId = HepsiburadaMarketPlaceId };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(new List<CategoryMarketplace> { categoryMatch });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("markası belirlenmemiş");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_Should_Return_Error_When_Barcode_Not_EAN13()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Test",
            CategoryId = 1,
            BrandId = 1,
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Barcode = "12345", // Not 13 chars
                    Images = new List<Image> { new() { StorageKey = "img.jpg" } }
                }
            }
        };

        var categoryMatch = new CategoryMarketplace { CategoryId = 1, MarketPlaceId = HepsiburadaMarketPlaceId };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(new List<CategoryMarketplace> { categoryMatch });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("EAN13");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_Should_Pass_For_Valid_Product()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Valid Product",
            CategoryId = 1,
            BrandId = 1,
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Barcode = "1234567890123",
                    Images = new List<Image> { new() { StorageKey = "img.jpg" } }
                }
            }
        };

        var categoryMatch = new CategoryMarketplace { CategoryId = 1, MarketPlaceId = HepsiburadaMarketPlaceId };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(new List<CategoryMarketplace> { categoryMatch });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeTrue();
    }
}
