using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Hepsiburada'dan kategori import işlemleri.
/// BaseCategoryImporterService'den türer ve Hepsiburada-specific işlemleri yapar.
/// HB kategorileri flat liste olarak gelir (leaf=true filtresiyle).
/// Attribute ID'leri string tipindedir.
/// </summary>
public class HepsiburadaCategoryImporter : BaseCategoryImporterService
{
    private readonly IHepsiburadaApiClient _apiClient;
    private readonly List<CategoryAttribute> _savedCategoryAttributes = new();
    private const string MarketplaceName = "Hepsiburada";

    /// <summary>
    /// Attribute value API rate limit: 50 req/dk → ~1.2 sn arası bırak
    /// </summary>
    private static readonly SemaphoreSlim _attributeValueThrottle = new(1, 1);
    private const int AttributeValueDelayMs = 1300;

    public override ImportSource Source => ImportSource.Hepsiburada;

    public HepsiburadaCategoryImporter(
        IHepsiburadaApiClient apiClient,
        IDbContextFactory<IntegrationDbContext> contextFactory,
        ILogger<HepsiburadaCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Hepsiburada API'sinden leaf kategorileri çeker (flat liste).
    /// GET /api/categories/get-all-categories?leaf=true&amp;status=ACTIVE&amp;available=true&amp;page={page}&amp;size=1000
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allCategories = new List<HepsiburadaCategoryDto>();
            var page = 0;
            var totalPages = 1;

            while (page < totalPages)
            {
                var url = $"/api/categories/get-all-categories?leaf=true&status=ACTIVE&available=true&page={page}&size=1000";
                var response = await _apiClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content
                    .ReadFromJsonAsync<HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaCategoryDto>>>(
                        cancellationToken: cancellationToken);

                if (apiResponse?.Success != true || apiResponse.Data?.Content == null)
                {
                    Logger.LogError("Hepsiburada kategori API'si başarısız yanıt döndü: {Code} {Message}",
                        apiResponse?.Code, apiResponse?.Message);
                    return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(
                        null!, $"Hepsiburada API hatası: {apiResponse?.Message}");
                }

                allCategories.AddRange(apiResponse.Data.Content);
                totalPages = apiResponse.Data.TotalPages;
                page++;
            }

            var categories = allCategories.Select(MapToExternalCategory).ToList();
            Logger.LogInformation("Hepsiburada'dan {Count} leaf kategori çekildi", categories.Count);
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Hepsiburada kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    private static ExternalCategoryDto MapToExternalCategory(HepsiburadaCategoryDto hbCategory)
    {
        return new ExternalCategoryDto
        {
            ExternalId = hbCategory.CategoryId.ToString(),
            Name = hbCategory.DisplayName ?? hbCategory.Name,
            ParentExternalId = hbCategory.ParentCategoryId?.ToString(),
            HasChildren = false, // leaf=true filtresi ile geliyorlar
            Children = new List<ExternalCategoryDto>()
        };
    }

    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, MarketplaceName, cancellationToken);
        _savedCategoryAttributes.Clear();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("{Source} kategorileri başarıyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} kategorileri import edilirken hata oluştu", Source);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Hepsiburada'ya özel kategori özellikleri import işlemi.
    /// 3 grup: baseAttributes (zorunlu temel), attributes (opsiyonel ürün), variantAttributes (varyant)
    /// Attribute ID'leri string tipinde.
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;

        try
        {
            var attrUrl = $"/api/categories/{category.ExternalCategoryId}/attributes";
            var response = await _apiClient.GetAsync(attrUrl);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content
                .ReadFromJsonAsync<HepsiburadaApiResponse<HepsiburadaAttributeData>>(
                    cancellationToken: cancellationToken);

            if (apiResponse?.Success != true || apiResponse.Data == null)
                return;

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            // baseAttributes — zorunlu temel özellikler
            await ProcessAttributeGroupAsync(dbContext, category, apiResponse.Data.BaseAttributes,
                isVarianter: false, categoryAttributeCategories, cancellationToken);

            // attributes — opsiyonel ürün özellikleri
            await ProcessAttributeGroupAsync(dbContext, category, apiResponse.Data.Attributes,
                isVarianter: false, categoryAttributeCategories, cancellationToken);

            // variantAttributes — varyant özellikleri
            await ProcessAttributeGroupAsync(dbContext, category, apiResponse.Data.VariantAttributes,
                isVarianter: true, categoryAttributeCategories, cancellationToken);

            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Hepsiburada kategori özellikleri import edilirken hata: {CategoryId}",
                category.ExternalCategoryId);
        }
    }

    private async Task ProcessAttributeGroupAsync(
        IntegrationDbContext dbContext,
        Category category,
        List<HepsiburadaAttributeDto> attributes,
        bool isVarianter,
        List<CategoryAttributeCategory> categoryAttributeCategories,
        CancellationToken cancellationToken)
    {
        if (attributes == null) return;

        foreach (var attr in attributes)
        {
            var dbCatAttr = await GetOrCreateCategoryAttributeAsync(dbContext, attr, cancellationToken);

            // Enum tipli attribute'lar için değerleri çek
            if (attr.Type == "enum" && !_savedCategoryAttributes.Any(x =>
                x.CategoryAttributeKey == attr.Id))
            {
                await ImportAttributeValuesAsync(dbContext, category.ExternalCategoryId!,
                    attr, dbCatAttr, cancellationToken);
                _savedCategoryAttributes.Add(dbCatAttr);
            }
            else if (!_savedCategoryAttributes.Any(x => x.CategoryAttributeKey == attr.Id))
            {
                _savedCategoryAttributes.Add(dbCatAttr);
            }

            categoryAttributeCategories.Add(new CategoryAttributeCategory
            {
                Category = category,
                CategoryAttribute = dbCatAttr,
                IsRequired = attr.Mandatory,
                IsVarianter = isVarianter,
                IsSlicer = false
            });
        }
    }

    private async Task<CategoryAttribute> GetOrCreateCategoryAttributeAsync(
        IntegrationDbContext dbContext,
        HepsiburadaAttributeDto attr,
        CancellationToken cancellationToken)
    {
        // Cache'de ara
        var cached = _savedCategoryAttributes.FirstOrDefault(x =>
            x.CategoryAttributeKey == attr.Id);
        if (cached != null) return cached;

        // DB'de string external ID ile ara
        var existing = await dbContext.CategoryAttributeMarketPlaceMatches
            .Include(m => m.ApplicationCategoryAttribute)
            .FirstOrDefaultAsync(m =>
                m.MarketPlaceCategoryAttributeExternalId == attr.Id &&
                m.MarketPlaceId == MarketPlace!.Id,
                cancellationToken);

        if (existing != null)
            return existing.ApplicationCategoryAttribute;

        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attr.Id,
            CategoryAttributeHumanized = attr.Name,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Marketplace eşleşmesi (string external ID ile)
        if (MarketPlace != null)
        {
            var match = new CategoryAttributeMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttribute = newAttr,
                MarketPlaceCategoryAttributeId = 0,
                MarketPlaceCategoryAttributeExternalId = attr.Id
            };
            await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);
        }

        return newAttr;
    }

    /// <summary>
    /// Enum tipli attribute değerlerini sayfalama ile çeker.
    /// Rate limit: 50 req/dk
    /// </summary>
    private async Task ImportAttributeValuesAsync(
        IntegrationDbContext dbContext,
        string categoryExternalId,
        HepsiburadaAttributeDto attr,
        CategoryAttribute dbCatAttr,
        CancellationToken cancellationToken)
    {
        var page = 0;
        var totalPages = 1;

        while (page < totalPages)
        {
            // Rate limiting
            await _attributeValueThrottle.WaitAsync(cancellationToken);
            try
            {
                var url = $"/api/categories/{categoryExternalId}/attribute/{attr.Id}/values?page={page}&size=1000";
                var response = await _apiClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning("Attribute values çekilemedi: {AttrId} Status: {Status}",
                        attr.Id, response.StatusCode);
                    break;
                }

                var apiResponse = await response.Content
                    .ReadFromJsonAsync<HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaAttributeValueDto>>>(
                        cancellationToken: cancellationToken);

                if (apiResponse?.Success != true || apiResponse.Data?.Content == null)
                    break;

                foreach (var attrValue in apiResponse.Data.Content)
                {
                    var value = new CategoryAttributeValue
                    {
                        Name = attrValue.Value,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    dbCatAttr.CategoryAttributeValues.Add(value);

                    if (MarketPlace != null)
                    {
                        var match = new CategoryAttributeValueMarketPlaceMatch
                        {
                            MarketPlace = MarketPlace,
                            ApplicationCategoryAttributeValue = value,
                            MarketPlaceCategoryAttributeValueId = 0,
                            MarketPlaceCategoryAttributeValueExternalId = attrValue.Id
                        };
                        await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(match, cancellationToken);
                    }
                }

                totalPages = apiResponse.Data.TotalPages;
                page++;

                // Rate limit delay
                if (page < totalPages)
                    await Task.Delay(AttributeValueDelayMs, cancellationToken);
            }
            finally
            {
                _attributeValueThrottle.Release();
            }
        }
    }
}
