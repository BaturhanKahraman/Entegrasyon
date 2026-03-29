using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.UnitTest.Business;

public class BrandServiceTests : BaseTest
{
    private readonly BrandService _sut;
    private readonly BrandMapper _brandMapper = new();
    private readonly TenantMemoryCache _tenantCache;

    public BrandServiceTests()
    {
        _tenantCache = new TenantMemoryCache(new MemoryCache(new MemoryCacheOptions()), mockTenantContext.Object);

        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<AddBrandDto>()))
            .Returns(Task.CompletedTask);

        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(new List<Brand>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new BrandService(
            MockValidator.Object,
            mockApplicationLogger.Object,
            _brandMapper,
            mockContextFactory.Object,
            _tenantCache);
    }

    [Fact]
    public async Task AddBrand_WithValidName_ReturnsSuccess()
    {
        // Arrange
        var dto = new AddBrandDto { Name = "Yeni Marka" };

        // Act
        var result = await _sut.AddBrand(dto);

        // Assert
        result.Success.Should().BeTrue();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddBrand_WithDuplicateName_ReturnsError()
    {
        // Arrange — mevcut markalar arasında aynı isimde var
        var existingBrands = new List<Brand>
        {
            new() { Id = 1, Name = "Mevcut Marka" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new AddBrandDto { Name = "Mevcut Marka" };

        // Act
        var result = await _sut.AddBrand(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten mevcut");
    }
}
