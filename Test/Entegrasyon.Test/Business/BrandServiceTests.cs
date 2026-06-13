using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

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
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<EditBrandDto>()))
            .Returns(Task.CompletedTask);

        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(new List<Brand>());
        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var notificationFlags = Options.Create(new NotificationFeatureFlags { PublishEnabled = false });
        var currentUser = new Mock<ICurrentUserContext>();
        _sut = new BrandService(
            MockValidator.Object,
            mockApplicationLogger.Object,
            _brandMapper,
            mockContextFactory.Object,
            _tenantCache,
            mockHybridCache.Object,
            mockTenantContext.Object,
            notificationFlags,
            currentUser.Object,
            new Mock<Microsoft.Extensions.Logging.ILogger<BrandService>>().Object);
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
        // Duplicate kontrolu DB unique kisitiyla (NormalizedName) ortusur; mevcut kayitlarin
        // NormalizedName'i backfill/Add ile dolu olur.
        var existingBrands = new List<Brand>
        {
            new() { Id = 1, Name = "Mevcut Marka", NormalizedName = "MEVCUT MARKA" }
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

    [Fact]
    public async Task UpdateBrand_OnlySeoSlugChanged_SameNameSameId_DoesNotReturnDuplicateError()
    {
        // Arrange — markanın kendi adı DB'de var; yalnızca SeoSlug değişiyor.
        // Self-exclude sayesinde "aynı isim" çakışması TETİKLENMEMELİ (kabul kriteri 3).
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", SeoSlug = "nike" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "Nike", "nike-turkiye");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeTrue();
        existingBrands.Single(b => b.Id == 7).SeoSlug.Should().Be("nike-turkiye");
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBrand_RenameToAnotherExistingBrandName_ReturnsDuplicateError()
    {
        // Arrange — 7 numaralı markayı, BAŞKA bir markanın (id 9) adına güncellemeye çalış.
        // Farklı Id + aynı isim → çakışma hatası DÖNMELİ (kabul kriteri 4).
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", NormalizedName = "NIKE", SeoSlug = "nike" },
            new() { Id = 9, Name = "Adidas", NormalizedName = "ADIDAS", SeoSlug = "adidas" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "Adidas", "nike-yeni");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten mevcut");
    }

    [Fact]
    public async Task UpdateBrand_BrandNotFound_ReturnsError()
    {
        // Arrange — verilen Id'de marka yok.
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(new List<Brand>());

        var dto = new EditBrandDto(404, "Yok", null);

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBrand_NameChanged_SetsNormalizedNameConsistentWithIndex()
    {
        // Arrange — ad değişince NormalizedName de DB index formülüyle (UPPER(TRIM(Name)))
        // tutarlı set edilmeli; aksi halde arama/eşleştirme bayat sonuç döner.
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", NormalizedName = "NIKE", SeoSlug = "nike" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "  puma  ", "puma");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeTrue();
        var brand = existingBrands.Single(b => b.Id == 7);
        brand.Name.Should().Be("  puma  ");
        brand.NormalizedName.Should().Be("PUMA");
    }

    [Fact]
    public async Task UpdateBrand_RenameDiffersOnlyByCaseOrWhitespace_FromAnotherBrand_ReturnsDuplicateError()
    {
        // Arrange — yeni ad mevcut başka markadan yalnızca büyük/küçük harf + boşlukla farklı.
        // App kontrolü Name exact yerine NormalizedName üzerinden olmalı ki DB unique
        // (IX_Brands_NormalizedName, UPPER/TRIM) kısıtıyla örtüşsün ve SaveChanges crash etmesin.
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", NormalizedName = "NIKE", SeoSlug = "nike" },
            new() { Id = 9, Name = "Adidas", NormalizedName = "ADIDAS", SeoSlug = "adidas" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "  adidas  ", "nike-yeni");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten mevcut");
    }

    [Fact]
    public async Task UpdateBrand_SeoSlugUsedByAnotherBrand_ReturnsError()
    {
        // Arrange — başka markada (id 9) kullanılan slug ile güncelleme.
        // DB IX_Brands_SeoSlug unique → app katmanında yakalanmalı, SaveChanges crash etmemeli.
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", NormalizedName = "NIKE", SeoSlug = "nike" },
            new() { Id = 9, Name = "Adidas", NormalizedName = "ADIDAS", SeoSlug = "adidas" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "Nike", "adidas");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("slug");
    }

    [Fact]
    public async Task UpdateBrand_KeepsOwnSeoSlug_DoesNotReturnSlugError()
    {
        // Arrange — marka kendi slug'ını koruyor; self-exclude sayesinde çakışma TETİKLENMEMELİ.
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", NormalizedName = "NIKE", SeoSlug = "nike" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "Nike", "nike");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBrand_EmptySeoSlug_SkipsUniquenessCheck()
    {
        // Arrange — slug boş; filtered index NULL'u çoklamaya izin verdiğinden benzersizlik
        // kontrolü ATLANMALI (mevcut davranışla tutarlı). Başka markada boş slug olsa bile geçer.
        var existingBrands = new List<Brand>
        {
            new() { Id = 7, Name = "Nike", NormalizedName = "NIKE", SeoSlug = "" },
            new() { Id = 9, Name = "Adidas", NormalizedName = "ADIDAS", SeoSlug = "" }
        };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(existingBrands);

        var dto = new EditBrandDto(7, "Nike", "");

        // Act
        var result = await _sut.UpdateBrand(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── DeleteBrand: ürün guard (LogicRunner business rule) ──────────────────

    [Fact]
    public async Task DeleteBrand_WhenBrandHasVisibleProducts_ReturnsError()
    {
        // Arrange — markaya bağlı görünür ürün(ler) var → silme reddedilmeli (kabul kriteri 1).
        var existingBrands = new List<Brand>
        {
            new() { Id = 5, Name = "Nike", NormalizedName = "NIKE" }
        };
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), BrandId = 5 },
            new() { Id = Guid.NewGuid(), BrandId = 5 }
        };
        mockIntegrationDbContext.Setup(x => x.Brands).ReturnsDbSet(existingBrands);
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);

        // Act
        var result = await _sut.DeleteBrand(5);

        // Assert — hata döner, mesaj formatı birebir, soft-delete yapılmaz, SaveChanges ÇAĞRILMAZ.
        result.Success.Should().BeFalse();
        result.Message.Should().Be("2 ürün bu markaya bağlı, önce taşıyın veya kaldırın");
        existingBrands.Single(b => b.Id == 5).IsDeleted.Should().BeFalse();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBrand_WhenBrandHasNoProducts_SoftDeletesAndReturnsSuccess()
    {
        // Arrange — markaya bağlı ürün yok → soft-delete başarıyla yapılmalı (kabul kriteri 2).
        var existingBrands = new List<Brand>
        {
            new() { Id = 5, Name = "Nike", NormalizedName = "NIKE", IsDeleted = false }
        };
        mockIntegrationDbContext.Setup(x => x.Brands).ReturnsDbSet(existingBrands);
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());

        // Act
        var result = await _sut.DeleteBrand(5);

        // Assert
        result.Success.Should().BeTrue();
        var brand = existingBrands.Single(b => b.Id == 5);
        brand.IsDeleted.Should().BeTrue();
        brand.DeletedAt.Should().NotBe(default);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RestoreBrand ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RestoreBrand_WhenBrandIsDeleted_SetsIsDeletedFalse()
    {
        // Arrange — soft-deleted marka geri yüklenince IsDeleted=false + DeletedAt=default (kabul kriteri 3).
        var deletedBrands = new List<Brand>
        {
            new() { Id = 5, Name = "Nike", NormalizedName = "NIKE", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow }
        };
        mockIntegrationDbContext.Setup(x => x.Brands).ReturnsDbSet(deletedBrands);

        // Act
        var result = await _sut.RestoreBrand(5);

        // Assert
        result.Success.Should().BeTrue();
        var brand = deletedBrands.Single(b => b.Id == 5);
        brand.IsDeleted.Should().BeFalse();
        brand.DeletedAt.Should().Be(default(DateTimeOffset));
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreBrand_WhenBrandNotFound_ReturnsError()
    {
        // Arrange — verilen Id'de silinmiş marka yok.
        mockIntegrationDbContext.Setup(x => x.Brands).ReturnsDbSet(new List<Brand>());

        // Act
        var result = await _sut.RestoreBrand(404);

        // Assert
        result.Success.Should().BeFalse();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestoreBrand_WhenActiveSameNamedBrandExists_ReturnsErrorAndDoesNotRestore()
    {
        // Arrange — arşivdeki "Nike" (id 5, IsDeleted=true) + aynı isimle eklenmiş AKTİF "Nike" (id 8).
        // Filtered unique index IX_Brands_NormalizedName (HasFilter "IsDeleted = false") nedeniyle
        // geri yükleme (IsDeleted=false) iki adet aktif "NIKE" yaratır → SaveChanges DbUpdateException → 500.
        // Business Rules adımı bunu önceden yakalayıp dostça ErrorResult dönmeli, SaveChanges ÇAĞRILMAMALI.
        var brands = new List<Brand>
        {
            new() { Id = 5, Name = "Nike", NormalizedName = "NIKE", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow },
            new() { Id = 8, Name = "Nike", NormalizedName = "NIKE", IsDeleted = false }
        };
        mockIntegrationDbContext.Setup(x => x.Brands).ReturnsDbSet(brands);

        // Act
        var result = await _sut.RestoreBrand(5);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten mevcut");
        brands.Single(b => b.Id == 5).IsDeleted.Should().BeTrue();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
