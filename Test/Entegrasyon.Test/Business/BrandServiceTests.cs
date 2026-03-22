using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using FluentAssertions;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.UnitTest.Business;

public class BrandServiceTests : BaseTest
{
    private readonly BrandService _sut;
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<IMemoryCache> _mockCache = new();

    public BrandServiceTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<AddBrandDto>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<AddBrandDto, Brand>(It.IsAny<AddBrandDto>()))
            .Returns((AddBrandDto dto) => new Brand { Name = dto.Name });

        // Cache always misses
        object? cacheValue = null;
        _mockCache
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);
        _mockCache
            .Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(new List<Brand>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new BrandService(
            MockValidator.Object,
            mockApplicationLogger.Object,
            _mockMapper.Object,
            mockContextFactory.Object,
            _mockCache.Object);
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
