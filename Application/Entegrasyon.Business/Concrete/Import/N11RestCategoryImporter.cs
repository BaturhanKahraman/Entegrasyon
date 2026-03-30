using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// N11 REST API üzerinden kategori ve kategori özelliklerini import eder.
/// SOAP versiyonunun REST karşılığıdır: tek istekle tam kategori ağacını çeker.
/// </summary>
public class N11RestCategoryImporter : BaseCategoryImporterService
{
    private readonly IN11RestClient _restClient;
    private const string MarketplaceName = "N11";

    public override ImportSource Source => ImportSource.N11;

    public N11RestCategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IN11RestClient restClient,
        ILogger<N11RestCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _restClient = restClient;
    }

    // -----------------------------------------------------------------------
    // GetExternalCategoriesAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// GET /cdn/categories ile tam kategori ağacını çeker ve düzleştirir.
    /// subCategories=null olan düğümler yaprak kategorilerdir.
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _restClient.GetAsync<List<N11CategoryTreeResponse>>("cdn/categories", cancellationToken);

            if (response is null || response.Count == 0)
            {
                Logger.LogCritical("N11 REST kategori ağacı boş döndü");
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, "N11 REST kategori API yanıtı boş döndü.");
            }

            var categories = response.Select(node => MapTreeNode(node)).ToList();
            Logger.LogInformation("N11 REST kategori ağacı çekildi — {Count} üst kategori", categories.Count);
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "N11 REST üst seviye kategoriler çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // ImportCategoriesAsync
    // -----------------------------------------------------------------------

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

            Logger.LogInformation("{Source} REST kategorileri başarıyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} REST kategorileri import edilirken hata oluştu", Source);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // ImportCategoryAttributesAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// GET /cdn/category/{id}/attribute ile kategori özelliklerini çeker ve kaydeder.
    /// </summary>
    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;
        if (MarketPlace is null) return;

        try
        {
            var response = await _restClient.GetAsync<N11CategoryAttributeResponse>(
                $"cdn/category/{category.ExternalCategoryId}/attribute", cancellationToken);

            if (response?.CategoryAttributes is null || response.CategoryAttributes.Count == 0) return;

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            foreach (var attr in response.CategoryAttributes)
            {
                var dbCatAttr = await GetOrCreateAttributeAsync(dbContext, attr, cancellationToken);

                if (attr.AttributeValues is { Count: > 0 })
                {
                    foreach (var val in attr.AttributeValues)
                    {
                        await AddAttributeValueAsync(dbContext, val, dbCatAttr, cancellationToken);
                    }
                }

                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr,
                    IsRequired = attr.IsMandatory,
                    IsSlicer = attr.IsSlicer,
                    IsVarianter = attr.IsVariant
                });
            }

            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "N11 REST kategori özellikleri import edilirken hata: {CategoryId}", category.ExternalCategoryId);
        }
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    private static ExternalCategoryDto MapTreeNode(N11CategoryTreeResponse node)
    {
        return new ExternalCategoryDto
        {
            ExternalId = node.Id.ToString(),
            Name = node.Name,
            HasChildren = node.SubCategories is { Count: > 0 },
            Children = node.SubCategories?.Select(MapTreeNode).ToList() ?? []
        };
    }

    private async Task<CategoryAttribute> GetOrCreateAttributeAsync(
        IntegrationDbContext dbContext,
        N11RestCategoryAttribute attr,
        CancellationToken cancellationToken)
    {
        var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .Include(m => m.ApplicationCategoryAttribute)
            .FirstOrDefaultAsync(
                m => m.MarketPlaceCategoryAttributeId == (int)attr.AttributeId
                  && m.MarketPlaceId == MarketPlace!.Id,
                cancellationToken);

        if (existingMatch is not null)
            return existingMatch.ApplicationCategoryAttribute;

        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attr.AttributeName,
            CategoryAttributeHumanized = attr.AttributeName,
            ImportId = (int)attr.AttributeId,
            AllowCustom = attr.IsCustomValue,
            CategoryAttributeValues = [],
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.CategoryAttributes.AddAsync(newAttr, cancellationToken);

        var match = new CategoryAttributeMarketPlaceMatch
        {
            MarketPlace = MarketPlace!,
            ApplicationCategoryAttribute = newAttr,
            MarketPlaceCategoryAttributeId = (int)attr.AttributeId
        };

        await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);
        return newAttr;
    }

    private async Task AddAttributeValueAsync(
        IntegrationDbContext dbContext,
        N11RestAttributeValue val,
        CategoryAttribute categoryAttribute,
        CancellationToken cancellationToken)
    {
        var value = new CategoryAttributeValue
        {
            Name = val.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categoryAttribute.CategoryAttributeValues.Add(value);

        if (MarketPlace is not null)
        {
            var valueMatch = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttributeValue = value,
                MarketPlaceCategoryAttributeValueId = (int)val.Id
            };
            await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(valueMatch, cancellationToken);
        }
    }
}
