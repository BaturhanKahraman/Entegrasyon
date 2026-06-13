using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.CategoryMatch;

/// <summary>
/// Unmap/silme öncesi sent-ürün uyarısını besleyen sayaç: yalnız BU kategorideki + BU pazaryerindeki
/// + GÖNDERİLMİŞ (Published) ürünler sayılır.
/// </summary>
public class CategoryMatchPublishedProductCountTests : BaseTest
{
    private CategoryMatchService CreateSut()
    {
        MockValidator = new Mock<IFluentValidator>();
        return new CategoryMatchService(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object);
    }

    [Fact]
    public async Task GetPublishedProductCountAsync_CountsOnlyPublishedInCategoryAndMarketplace()
    {
        // Arrange
        var inCategory = new Product { Id = Guid.NewGuid(), CategoryId = 5 };
        var otherCategory = new Product { Id = Guid.NewGuid(), CategoryId = 9 };

        IList<ProductMarketplace> productMarketplaces =
        [
            new() { MarketPlaceId = 1, Status = MarketplaceProductStatus.Published, Product = inCategory },    // ✓ sayılır
            new() { MarketPlaceId = 1, Status = MarketplaceProductStatus.Pending,   Product = inCategory },    // pending → hayır
            new() { MarketPlaceId = 2, Status = MarketplaceProductStatus.Published, Product = inCategory },    // farklı pazaryeri → hayır
            new() { MarketPlaceId = 1, Status = MarketplaceProductStatus.Published, Product = otherCategory }, // farklı kategori → hayır
        ];
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(productMarketplaces);

        var sut = CreateSut();

        // Act
        var count = await sut.GetPublishedProductCountAsync(categoryId: 5, marketPlaceId: 1);

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetMappedCategoryIdsAsync_ReturnsOnlyActiveMappedCategoryIds()
    {
        // Arrange
        IList<Entegrasyon.Entity.Categories.CategoryMarketplace> mappings =
        [
            new() { CategoryId = 5, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 7, MarketPlaceId = 2, IsActive = true },
            new() { CategoryId = 5, MarketPlaceId = 2, IsActive = true },   // dup kategori → distinct
            new() { CategoryId = 9, MarketPlaceId = 1, IsActive = false },  // pasif → hariç
        ];
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(mappings);

        var sut = CreateSut();

        // Act
        var ids = await sut.GetMappedCategoryIdsAsync();

        // Assert
        ids.Should().BeEquivalentTo(new[] { 5, 7 });
    }
}
