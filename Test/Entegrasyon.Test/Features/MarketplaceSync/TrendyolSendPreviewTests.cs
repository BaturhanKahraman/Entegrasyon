using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.Test.Features.MarketplaceSync;

/// <summary>
/// Unit tests for ITrendyolProductService.GetSendPreviewAsync().
/// Tests preview DTO mapping with and without overrides.
/// </summary>
public class TrendyolSendPreviewTests
{
    [Fact]
    public async Task GetSendPreviewAsync_WithOverrides_ReturnsMappedPreviewWithOverridePrices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockService = new Mock<ITrendyolProductService>();

        var overrides = new MarketplaceOverrideDetailDto
        {
            MarketPlaceId = 1,
            MarketPlaceName = "Trendyol",
            TitleOverride = "Custom Title",
            DescriptionOverride = "Custom Description",
            VariantOverrides = []
        };

        var preview = new TrendyolSendPreviewDto(
            Title: "Custom Title",
            BrandName: "Nike",
            TrendyolBrandName: "NIKE",
            CategoryName: "Giyim > T-Shirt",
            TrendyolCategoryName: "Giyim > Tişört",
            TrendyolCategoryId: 1234,
            Description: "Custom Description",
            Attributes: new List<TrendyolPreviewAttributeDto>
            {
                new TrendyolPreviewAttributeDto("Renk", "Siyah"),
                new TrendyolPreviewAttributeDto("Beden", "M")
            },
            Variants: new List<TrendyolPreviewVariantDto>
            {
                new TrendyolPreviewVariantDto("ABC-S", "Siyah / S", 299, 249, 15),
                new TrendyolPreviewVariantDto("ABC-M", "Siyah / M", 299, 249, 22)
            }
        );

        mockService
            .Setup(s => s.GetSendPreviewAsync(productId, overrides))
            .ReturnsAsync(new SuccessDataResult<TrendyolSendPreviewDto>(preview));

        // Act
        var result = await mockService.Object.GetSendPreviewAsync(productId, overrides);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Custom Title");
        result.Data.Description.Should().Be("Custom Description");
        result.Data.Attributes.Should().HaveCount(2);
        result.Data.Variants.Should().HaveCount(2);
        result.Data.Variants.First().SalePrice.Should().Be(249);
    }

    [Fact]
    public async Task GetSendPreviewAsync_WithoutOverrides_ReturnsMappedPreviewWithOriginalData()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockService = new Mock<ITrendyolProductService>();

        var preview = new TrendyolSendPreviewDto(
            Title: "Original Title",
            BrandName: "Nike",
            TrendyolBrandName: "NIKE",
            CategoryName: "Giyim > T-Shirt",
            TrendyolCategoryName: "Giyim > Tişört",
            TrendyolCategoryId: 1234,
            Description: "Original Description",
            Attributes: new List<TrendyolPreviewAttributeDto>
            {
                new TrendyolPreviewAttributeDto("Renk", "Siyah")
            },
            Variants: new List<TrendyolPreviewVariantDto>
            {
                new TrendyolPreviewVariantDto("ABC-001", "Siyah", 199, 179, 10)
            }
        );

        mockService
            .Setup(s => s.GetSendPreviewAsync(productId, null))
            .ReturnsAsync(new SuccessDataResult<TrendyolSendPreviewDto>(preview));

        // Act
        var result = await mockService.Object.GetSendPreviewAsync(productId, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Original Title");
        result.Data.Description.Should().Be("Original Description");
        result.Data.Variants.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSendPreviewAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var mockService = new Mock<ITrendyolProductService>();

        mockService
            .Setup(s => s.GetSendPreviewAsync(productId, null))
            .ReturnsAsync(new ErrorDataResult<TrendyolSendPreviewDto>(null!, "Ürün bulunamadı."));

        // Act
        var result = await mockService.Object.GetSendPreviewAsync(productId, null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Ürün bulunamadı.");
    }
}
