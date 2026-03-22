using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// Pazarama pazaryerinden REST API üzerinden kategori ve kategori özelliklerini import eder.
/// API düz liste döndürür (parentId ile hiyerarşi); tek geçişli dictionary algoritması ile
/// ağaç yapısına dönüştürülür.
/// </summary>
public class PazaramaCategoryImporter : BaseCategoryImporterService
{
    private readonly IPazaramaApiClient _apiClient;
    private const string MarketplaceName = "Pazarama";

    public override ImportSource Source => ImportSource.Pazarama;

    public PazaramaCategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IPazaramaApiClient apiClient,
        ILogger<PazaramaCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Pazarama API'sinden tüm kategorileri düz liste olarak çeker ve ağaç yapısına dönüştürür.
    /// GET /category/getCategoryTree — parentId, leaf boolean ile düz liste döner.
    /// Tek geçişli dictionary algoritması ile O(n) karmaşıklıkta ağaç inşa edilir.
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetAsync("/category/getCategoryTree");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content
                .ReadFromJsonAsync<PazaramaResponse<List<PazaramaCategoryDto>>>(
                    cancellationToken: cancellationToken);

            var flatList = apiResponse?.Data ?? new List<PazaramaCategoryDto>();

            var tree = BuildTreeFromFlatList(flatList);

            Logger.LogInformation("Pazarama'dan {Count} kök kategori oluşturuldu ({Total} toplam)",
                tree.Count, flatList.Count);

            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(tree);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Pazarama kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null, $"Hata: {ex.Message}");
        }
    }

    /// <summary>
    /// Import işleminden önce Pazarama marketplace kaydını yükler, ardından temel sınıfın
    /// transaction döngüsünü çalıştırır.
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, MarketplaceName, cancellationToken);

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
    /// Pazarama'ya özgü kategori özellikleri import işlemi.
    /// GET /category/getCategoryWithAttributes?Id={guid} — attribute ve value GUID'leri string olarak saklanır.
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;
        if (MarketPlace == null) return;

        try
        {
            var url = $"/category/getCategoryWithAttributes?Id={category.ExternalCategoryId}";
            var response = await _apiClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content
                .ReadFromJsonAsync<PazaramaResponse<PazaramaCategoryWithAttributesDto>>(
                    cancellationToken: cancellationToken);

            var categoryWithAttrs = apiResponse?.Data;
            if (categoryWithAttrs?.Attributes == null || !categoryWithAttrs.Attributes.Any())
                return;

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            foreach (var attr in categoryWithAttrs.Attributes)
            {
                var dbCatAttr = await GetOrCreateAttributeAsync(dbContext, attr, cancellationToken);

                foreach (var val in attr.AttributeValues)
                {
                    await AddAttributeValueAsync(dbContext, val, dbCatAttr, cancellationToken);
                }

                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr,
                    IsRequired = attr.IsRequired,
                    IsVarianter = attr.IsVariantable,
                    IsSlicer = false
                });
            }

            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Pazarama kategori özellikleri import edilirken hata: {CategoryId}",
                category.ExternalCategoryId);
        }
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    /// <summary>
    /// Düz liste → ağaç dönüşümü: tek geçişli O(n) dictionary algoritması.
    /// - Tüm öğeler önce dictionary'e alınır
    /// - Her öğe: parentId varsa ebeveynin Children listesine eklenir; yoksa kök olarak işaretlenir
    /// - ParentId belirtilmiş ama dictionary'de bulunmayan öğeler yetim olarak kök listesine alınır ve uyarı loglanır
    /// </summary>
    private List<ExternalCategoryDto> BuildTreeFromFlatList(List<PazaramaCategoryDto> flatList)
    {
        var dtoMap = new Dictionary<Guid, ExternalCategoryDto>(flatList.Count);
        var roots = new List<ExternalCategoryDto>();

        // 1. adım: Tüm öğeleri DTO'ya dönüştür ve dictionary'e ekle
        foreach (var item in flatList)
        {
            dtoMap[item.Id] = new ExternalCategoryDto
            {
                ExternalId = item.Id.ToString(),
                Name = item.DisplayName ?? item.Name,
                HasChildren = !item.Leaf,
                Children = new List<ExternalCategoryDto>()
            };
        }

        // 2. adım: Ebeveyn-çocuk ilişkisini kur; kök ve yetim öğeleri tespit et
        foreach (var item in flatList)
        {
            var dto = dtoMap[item.Id];

            if (item.ParentId == null)
            {
                // Kök kategori — parentId yok
                roots.Add(dto);
            }
            else if (dtoMap.TryGetValue(item.ParentId.Value, out var parentDto))
            {
                // Ebeveyn bulundu — çocuk olarak ekle
                parentDto.Children.Add(dto);
            }
            else
            {
                // Yetim kategori — parentId var ama listede yok → kök olarak işle
                Logger.LogWarning(
                    "Pazarama yetim kategori tespit edildi (Id={CategoryId}, ParentId={ParentId}), kök olarak işleniyor",
                    item.Id, item.ParentId);
                roots.Add(dto);
            }
        }

        return roots;
    }

    /// <summary>
    /// Marketplace-aware dedup: Pazarama attribute GUID'ine göre var olan attribute'u bulur
    /// ya da yeni oluşturur. String external ID (MarketPlaceCategoryAttributeExternalId) ile eşleştirilir.
    /// </summary>
    private async Task<CategoryAttribute> GetOrCreateAttributeAsync(
        IntegrationDbContext dbContext,
        PazaramaCategoryAttributeDto attr,
        CancellationToken cancellationToken)
    {
        var guidString = attr.Id.ToString();

        var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .Include(m => m.ApplicationCategoryAttribute)
            .FirstOrDefaultAsync(m =>
                m.MarketPlaceCategoryAttributeExternalId == guidString &&
                m.MarketPlaceId == MarketPlace!.Id,
                cancellationToken);

        if (existingMatch != null)
            return existingMatch.ApplicationCategoryAttribute;

        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attr.Name,
            CategoryAttributeHumanized = attr.DisplayName ?? attr.Name,
            AllowCustom = false,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var match = new CategoryAttributeMarketPlaceMatch
        {
            MarketPlace = MarketPlace!,
            ApplicationCategoryAttribute = newAttr,
            MarketPlaceCategoryAttributeId = 0,
            MarketPlaceCategoryAttributeExternalId = guidString
        };

        await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);

        return newAttr;
    }

    /// <summary>
    /// CategoryAttributeValue ve CategoryAttributeValueMarketPlaceMatch kaydını ekler.
    /// Value GUID'i string olarak MarketPlaceCategoryAttributeValueExternalId'de saklanır.
    /// </summary>
    private async Task AddAttributeValueAsync(
        IntegrationDbContext dbContext,
        PazaramaCategoryAttributeValueDto val,
        CategoryAttribute categoryAttribute,
        CancellationToken cancellationToken)
    {
        var value = new CategoryAttributeValue
        {
            Name = val.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categoryAttribute.CategoryAttributeValues.Add(value);

        if (MarketPlace != null)
        {
            var valueMatch = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttributeValue = value,
                MarketPlaceCategoryAttributeValueId = 0,
                MarketPlaceCategoryAttributeValueExternalId = val.Id.ToString()
            };
            await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(valueMatch, cancellationToken);
        }
    }
}
