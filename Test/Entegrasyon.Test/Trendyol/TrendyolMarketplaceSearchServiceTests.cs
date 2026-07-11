using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Test.Fixtures;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolMarketplaceSearchService unit tests — verifies category search (with flattening),
/// brand search (Trendyol API + DB fallback), and attribute search.
/// WireMock pattern: yalnizca brand search HTTP cagirdigi icin 2 test stub kurar;
/// digerleri DB veya category importer mock'u kullanir.
/// </summary>
[Collection(WireMockCollection.Name)]
public class TrendyolMarketplaceSearchServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly WireMockFixture _wm;
    private readonly Mock<ITrendyolCategoryImportService> _categoryImportMock = new();
    private readonly Mock<ILogger<TrendyolMarketplaceSearchService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ITrendyolAttributeCatalog> _attributeCatalogMock = new();
    private readonly Mock<IMasterBrandSearchProvider> _masterBrandMock = new();
    private readonly Mock<ILogger<TrendyolApiClient>> _apiClientLoggerMock = new();

    public TrendyolMarketplaceSearchServiceTests(WireMockFixture wm)
    {
        _wm = wm;
        _wm.ResetAll();

        // Gerçek TrendyolApiClient kullanılır: BaseAddress'i DB'deki MarketPlace.BaseUrl'den
        // alır (WireMock'a işaret eder). Factory yalnızca çıplak HttpClient verir —
        // hard-coded named-client BaseAddress'i maskelemesin diye burada set ETMİYORUZ.
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient());

        // Fallback default: boş master sonuç — testler gerektiğinde override eder.
        _masterBrandMock
            .Setup(m => m.SearchBrandsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceBrandSearchResult>>([]));
    }

    private TrendyolMarketplaceSearchService CreateSut() => new(
        mockContextFactory.Object,
        _categoryImportMock.Object,
        new TrendyolApiClient(mockContextFactory.Object, _httpClientFactoryMock.Object, _apiClientLoggerMock.Object),
        _masterBrandMock.Object,
        _attributeCatalogMock.Object,
        _loggerMock.Object);

    /// <summary>
    /// DB'de BaseUrl'i WireMock'a işaret eden Trendyol marketplace kaydı kurar.
    /// </summary>
    private void SetupTrendyolMarketPlace()
    {
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>
            {
                new()
                {
                    Id = 1, Name = "Trendyol",
                    ApiKey = "test-api-key", ApiSecret = "test-api-secret",
                    SellerId = "12345", BaseUrl = _wm.BaseUrl
                }
            });
    }

    /// <summary>
    /// Brand search endpoint stub'u — Trendyol API'nin brand search cevabini taklit eder.
    /// </summary>
    private void StubBrandSearch(int statusCode, string body)
    {
        _wm.Server
            .Given(Request.Create().WithPath("/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchCategoriesAsync — HTTP'siz, importer mock'u kullanir
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchCategoriesAsync_ReturnsMatchingCategories()
    {
        // Arrange
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Giyim", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Erkek Giyim" },
                new() { Id = 3, Name = "Kadin Giyim" }
            }},
            new() { Id = 4, Name = "Elektronik" }
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "Erkek");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("Erkek Giyim");
    }

    [Fact]
    public async Task SearchCategoriesAsync_EmptyQuery_ReturnsAllFlattened()
    {
        // Arrange
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Giyim", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Erkek Giyim" }
            }},
            new() { Id = 3, Name = "Elektronik" }
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3); // Giyim, Erkek Giyim, Elektronik
    }

    [Fact]
    public async Task SearchCategoriesAsync_LimitsResultsTo20()
    {
        // Arrange
        var categories = Enumerable.Range(1, 30)
            .Select(i => new ImportedTrendyolCategory { Id = i, Name = $"Category {i}" })
            .ToList();

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(20);
    }

    [Fact]
    public async Task SearchCategoriesAsync_WhenImportServiceFails_ReturnsError()
    {
        // Arrange
        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new ErrorDataResult<IEnumerable<ImportedTrendyolCategory>>([], "API hatasi"));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "test");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SearchCategoriesAsync_WhenExceptionThrown_ReturnsError()
    {
        // Arrange
        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "test");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("hata");
    }

    [Fact]
    public async Task SearchCategoriesAsync_FullPathIncludesParent()
    {
        // Arrange
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Giyim", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Erkek", SubCategories = new List<ImportedTrendyolCategory>
                {
                    new() { Id = 3, Name = "Tisort" }
                }}
            }}
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "Tisort");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].FullPath.Should().Be("Giyim > Erkek > Tisort");
    }

    [Fact]
    public async Task SearchCategoriesAsync_MarksLeafAndParentCategories()
    {
        // Arrange — "Oyuncak" parent (alt kategorisi var) + "Peluş Oyuncak" leaf
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Oyuncak", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Peluş Oyuncak" }
            }}
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "");

        // Assert — parent leaf değil, child leaf
        result.Success.Should().BeTrue();
        result.Data.Single(c => c.Id == 1).IsLeaf.Should().BeFalse();
        result.Data.Single(c => c.Id == 2).IsLeaf.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchBrandsAsync — Trendyol API (WireMock stub)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchBrandsAsync_Trendyol_ReturnsMatchingBrands()
    {
        // Arrange
        SetupTrendyolMarketPlace();
        var trendyolResponse = new
        {
            brands = new[] { new { id = 111, name = "Nike" } }
        };
        StubBrandSearch(200, JsonSerializer.Serialize(trendyolResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(1, "Nike");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Id.Should().Be(111);
        result.Data[0].Name.Should().Be("Nike");
    }

    [Fact]
    public async Task SearchBrandsAsync_Trendyol_CallsApiThroughMarketplaceBaseUrl()
    {
        // Arrange — kök sebep regresyonu (T-selectbox-boş): istek DB'deki BaseUrl'e
        // (WireMock) auth header'la gitmeli; hard-coded gateway'e değil.
        SetupTrendyolMarketPlace();
        StubBrandSearch(200, JsonSerializer.Serialize(new { brands = Array.Empty<object>() }));

        var sut = CreateSut();

        // Act
        await sut.SearchBrandsAsync(1, "Nike");

        // Assert — WireMock isteği gördü + Basic Auth taşıyor
        var log = _wm.Server.LogEntries.Single();
        log.RequestMessage!.Path.Should().Contain("/product/brands/by-name");
        log.RequestMessage!.Headers.Should().ContainKey("Authorization");
    }

    [Fact]
    public async Task SearchBrandsAsync_ApiEmpty_FallsBackToMasterCatalog()
    {
        // Arrange — API başarılı ama boş → master katalog fallback devreye girer
        SetupTrendyolMarketPlace();
        StubBrandSearch(200, JsonSerializer.Serialize(new { brands = Array.Empty<object>() }));

        _masterBrandMock
            .Setup(m => m.SearchBrandsAsync(1, "Nike", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceBrandSearchResult>>(
                [new(555, "Nike (Master)")]));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(1, "Nike");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(555);
        result.Data[0].Name.Should().Be("Nike (Master)");
    }

    [Fact]
    public async Task SearchBrandsAsync_ApiError_FallsBackToMasterCatalog()
    {
        // Arrange — API 500 → fallback sonuçları dönmeli
        SetupTrendyolMarketPlace();
        StubBrandSearch((int)HttpStatusCode.InternalServerError, "");

        _masterBrandMock
            .Setup(m => m.SearchBrandsAsync(1, "Nike", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceBrandSearchResult>>(
                [new(777, "Adidas (Master)")]));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(1, "Nike");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(777);
    }

    [Fact]
    public async Task SearchBrandsAsync_ApiErrorAndMasterEmpty_ReturnsError()
    {
        // Arrange — API 500 + master boş → hata sinyali korunur
        SetupTrendyolMarketPlace();
        StubBrandSearch((int)HttpStatusCode.InternalServerError, "");

        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(1, "Nike");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SearchBrandsAsync_EmptyQueryApiError_DoesNotFallBackToMaster()
    {
        // Arrange — BrandAutoMatchService "tüm markalar" için boş query kullanır
        // (BrandAutoMatchService.cs:32). Master fallback OrderBy'sız Take(20) döndüğü için
        // nondeterministik bir alt küme — bunu "tüm katalog" sanıp otomatik eşleştirme/kayıt
        // yapmak yanlış marka eşleşmelerini sessizce DB'ye yazar. Boş query'de fallback'e
        // hiç düşülmemeli; API hatası ErrorDataResult olarak yukarı taşınmalı.
        SetupTrendyolMarketPlace();
        StubBrandSearch((int)HttpStatusCode.InternalServerError, "");

        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(1, "");

        // Assert
        result.Success.Should().BeFalse();
        _masterBrandMock.Verify(
            m => m.SearchBrandsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchBrandsAsync_ApiHasResults_DoesNotCallMasterCatalog()
    {
        // Arrange
        SetupTrendyolMarketPlace();
        StubBrandSearch(200, JsonSerializer.Serialize(new
        {
            brands = new[] { new { id = 111, name = "Nike" } }
        }));

        var sut = CreateSut();

        // Act
        await sut.SearchBrandsAsync(1, "Nike");

        // Assert
        _masterBrandMock.Verify(
            m => m.SearchBrandsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchBrandsAsync_NonTrendyol_FallsBackToDb()
    {
        // Arrange — marketPlaceId=2 (N11), no HTTP mock needed
        var matches = new List<BrandMarketPlaceMatch>
        {
            new() { MarketPlaceId = 2, MarketPlaceBrandId = 50,
                    MarketPlaceBrandExternalId = "Nike TR" },
            new() { MarketPlaceId = 2, MarketPlaceBrandId = 51,
                    MarketPlaceBrandExternalId = "Adidas" },
            new() { MarketPlaceId = 3, MarketPlaceBrandId = 99,
                    MarketPlaceBrandExternalId = "Nike" }, // different marketplace, excluded
        };
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(matches);

        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(2, "Nike");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("Nike TR");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAttributesAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAttributesAsync_Trendyol_DelegatesToCatalog()
    {
        _attributeCatalogMock
            .Setup(c => c.SearchAttributesAsync("Renk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MarketplaceAttributeSearchResult> { new(348, "Renk") });

        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync(1, "Renk");

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(348);
    }

    [Fact]
    public async Task SearchAttributesAsync_NonTrendyol_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.SearchAttributesAsync(2, "Renk");

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAttributeValuesAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAttributeValuesAsync_Trendyol_DelegatesToCatalog()
    {
        _attributeCatalogMock
            .Setup(c => c.SearchValuesAsync(348, "Kır", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MarketplaceOption> { new(1002, "Kırmızı") });

        var sut = CreateSut();

        var result = await sut.SearchAttributeValuesAsync(1, marketplaceAttributeId: 348, query: "Kır");

        result.Success.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(1002);
    }

    [Fact]
    public async Task SearchAttributeValuesAsync_NonTrendyol_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.SearchAttributeValuesAsync(2, marketplaceAttributeId: 348, query: "Kır");

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
