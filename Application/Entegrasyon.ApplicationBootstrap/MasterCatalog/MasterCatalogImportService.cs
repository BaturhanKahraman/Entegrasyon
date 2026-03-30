using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.MasterCatalog;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.ApplicationBootstrap.MasterCatalog;

/// <summary>
/// Master catalog (AdminPanelDb) içeriğini tenant'ın IntegrationDb'sine aktarır.
/// Her iki DbContext'e erişim için IDbContextFactory kullanılır.
/// </summary>
public class MasterCatalogImportService(
    IDbContextFactory<AdminPanelDbContext> adminDbFactory,
    IDbContextFactory<IntegrationDbContext> integrationDbFactory,
    ILogger<MasterCatalogImportService> logger) : IMasterCatalogImportService
{
    public async Task<ImportResultDto> ImportFromMasterAsync(
        int tenantId,
        IList<int> masterCategoryIds,
        CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);
        await using var integrationDb = await integrationDbFactory.CreateDbContextAsync(ct);

        int categoriesImported = 0, attributesImported = 0, valuesImported = 0, mappingsImported = 0;
        int categoriesSkipped = 0, attributesSkipped = 0, valuesSkipped = 0;

        // Seçilen master kategori ID'leri ile tüm alt ağacı topla
        var allMasterCategoryIds = await ExpandCategoryIdsAsync(adminDb, masterCategoryIds, ct);

        var masterCategories = await adminDb.MasterCategories
            .Include(c => c.MarketplaceMappings)
            .Include(c => c.CategoryAttributes)
                .ThenInclude(ca => ca.MasterAttribute)
                    .ThenInclude(a => a.Values)
                        .ThenInclude(v => v.MarketplaceMappings)
            .Include(c => c.CategoryAttributes)
                .ThenInclude(ca => ca.MasterAttribute)
                    .ThenInclude(a => a.MarketplaceMappings)
            .Where(c => allMasterCategoryIds.Contains(c.Id))
            .OrderBy(c => c.ParentId == null ? 0 : 1) // kökler önce
            .ThenBy(c => c.SortOrder)
            .ToListAsync(ct);

        // Marketplace tablosu (Trendyol)
        var trendyolMarketPlace = await integrationDb.MarketPlaces
            .AsTracking()
            .FirstOrDefaultAsync(m => m.Id == 1, ct);

        // Attribute deduplication cache: MasterAttributeId → CategoryAttribute
        var attributeCache = new Dictionary<int, CategoryAttribute>();

        await using var transaction = await integrationDb.Database.BeginTransactionAsync(ct);
        try
        {
            // Kategori ID → Category map (yeni oluşturulanlar için parent link)
            var categoryMap = new Dictionary<int, Category>();

            foreach (var masterCat in masterCategories)
            {
                var existingCategory = await integrationDb.Categories
                    .FirstOrDefaultAsync(c => c.ExternalCategoryId == masterCat.OriginalExternalId, ct);

                if (existingCategory != null)
                {
                    categoryMap[masterCat.Id] = existingCategory;
                    categoriesSkipped++;
                    continue;
                }

                // Üst kategoriyi bul
                Category? superCategory = null;
                if (masterCat.ParentId.HasValue && categoryMap.TryGetValue(masterCat.ParentId.Value, out var parentCat))
                    superCategory = parentCat;

                var newCategory = new Category
                {
                    Name = masterCat.Name,
                    IsImported = true,
                    ImportSource = Entity.Categories.ImportSource.Trendyol,
                    ExternalCategoryId = masterCat.OriginalExternalId,
                    SuperCategory = superCategory
                };

                await integrationDb.Categories.AddAsync(newCategory, ct);

                // CategoryMarketplaceMatch
                foreach (var mapping in masterCat.MarketplaceMappings)
                {
                    if (trendyolMarketPlace != null)
                    {
                        if (int.TryParse(mapping.ExternalCategoryId, out int extId))
                        {
                            var categoryMatch = new CategoryMarketPlaceMatch
                            {
                                MarketPlace = trendyolMarketPlace,
                                ApplicationCategory = newCategory,
                                MarketPlaceCategoryId = extId
                            };
                            await integrationDb.CategoryMarketPlaceMatches.AddAsync(categoryMatch, ct);
                            mappingsImported++;
                        }
                    }
                }

                categoryMap[masterCat.Id] = newCategory;
                categoriesImported++;

                // Attribute'lar (sadece yaprak kategoriler için)
                if (masterCat.IsLeaf)
                {
                    foreach (var catAttr in masterCat.CategoryAttributes)
                    {
                        var masterAttr = catAttr.MasterAttribute;
                        CategoryAttribute categoryAttribute;

                        if (attributeCache.TryGetValue(masterAttr.Id, out var cachedAttr))
                        {
                            categoryAttribute = cachedAttr;
                        }
                        else
                        {
                            // Mevcut attribute kontrolü (ImportId üzerinden)
                            var existingAttr = await integrationDb.CategoryAttributes
                                .Include(a => a.CategoryAttributeValues)
                                .FirstOrDefaultAsync(a => a.ImportId == masterAttr.Id, ct);

                            if (existingAttr != null)
                            {
                                categoryAttribute = existingAttr;
                                attributeCache[masterAttr.Id] = categoryAttribute;
                                attributesSkipped++;
                            }
                            else
                            {
                                categoryAttribute = new CategoryAttribute
                                {
                                    CategoryAttributeKey = masterAttr.Key,
                                    CategoryAttributeHumanized = masterAttr.HumanizedName,
                                    AllowCustom = masterAttr.AllowCustom,
                                    ImportId = masterAttr.Id
                                };

                                await integrationDb.CategoryAttributes.AddAsync(categoryAttribute, ct);
                                attributesImported++;

                                // Attribute marketplace mapping
                                foreach (var attrMapping in masterAttr.MarketplaceMappings)
                                {
                                    if (trendyolMarketPlace != null && int.TryParse(attrMapping.ExternalAttributeId, out int extAttrId))
                                    {
                                        await integrationDb.CategoryAttributeMarketPlaceMatches.AddAsync(
                                            new CategoryAttributeMarketPlaceMatch
                                            {
                                                MarketPlace = trendyolMarketPlace,
                                                ApplicationCategoryAttribute = categoryAttribute,
                                                MarketPlaceCategoryAttributeId = extAttrId
                                            }, ct);
                                        mappingsImported++;
                                    }
                                }

                                // Attribute değerleri
                                foreach (var masterVal in masterAttr.Values)
                                {
                                    var attrValue = new CategoryAttributeValue
                                    {
                                        Name = masterVal.Name
                                    };
                                    categoryAttribute.CategoryAttributeValues.Add(attrValue);
                                    valuesImported++;

                                    // Value marketplace mapping
                                    foreach (var valMapping in masterVal.MarketplaceMappings)
                                    {
                                        if (trendyolMarketPlace != null && int.TryParse(valMapping.ExternalValueId, out int extValId))
                                        {
                                            await integrationDb.CategoryAttributeValueMarketPlaceMatches.AddAsync(
                                                new CategoryAttributeValueMarketPlaceMatch
                                                {
                                                    MarketPlace = trendyolMarketPlace,
                                                    ApplicationCategoryAttributeValue = attrValue,
                                                    MarketPlaceCategoryAttributeValueId = extValId
                                                }, ct);
                                            mappingsImported++;
                                        }
                                    }
                                }

                                attributeCache[masterAttr.Id] = categoryAttribute;
                            }
                        }

                        // Junction kaydı: CategoryAttributeCategory
                        bool junctionExists = await integrationDb.CategoryAttributeCategories
                            .AnyAsync(j => j.Category == newCategory && j.CategoryAttribute == categoryAttribute, ct);

                        if (!junctionExists)
                        {
                            await integrationDb.CategoryAttributeCategories.AddAsync(
                                new CategoryAttributeCategory
                                {
                                    Category = newCategory,
                                    CategoryAttribute = categoryAttribute,
                                    IsRequired = catAttr.IsRequired,
                                    IsVarianter = catAttr.IsVarianter,
                                    IsSlicer = catAttr.IsSlicer
                                }, ct);
                        }
                    }
                }
            }

            await integrationDb.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Master catalog import tamamlandı (tenantId={TenantId}): " +
                "{CatImport} kategori, {AttrImport} attribute, {ValImport} değer, {MapImport} eşleştirme eklendi. " +
                "{CatSkip} kategori, {AttrSkip} attribute, {ValSkip} değer atlandı.",
                tenantId, categoriesImported, attributesImported, valuesImported, mappingsImported,
                categoriesSkipped, attributesSkipped, valuesSkipped);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Master catalog import hatası (tenantId={TenantId}).", tenantId);
            throw;
        }

        return new ImportResultDto(
            categoriesImported, attributesImported, valuesImported, mappingsImported,
            categoriesSkipped, attributesSkipped, valuesSkipped);
    }

    public async Task<IList<MasterCategoryTreeDto>> GetMasterCategoryTreeAsync(CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);

        var allCategories = await adminDb.MasterCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new MasterCategoryTreeDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                IsLeaf = c.IsLeaf,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder
            })
            .ToListAsync(ct);

        return BuildTree(allCategories, null);
    }

    public async Task<IList<SectorPackageDto>> GetSectorPackagesAsync(CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);

        var packages = await adminDb.SectorPackages
            .Where(s => s.IsActive)
            .Include(s => s.Categories)
            .Select(s => new SectorPackageDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IconName = s.IconName,
                CategoryCount = s.Categories.Count,
                MasterCategoryIds = s.Categories.Select(c => c.MasterCategoryId).ToList()
            })
            .ToListAsync(ct);

        return packages;
    }

    public async Task<IList<int>> GetSectorPackageCategoryIdsAsync(int sectorPackageId, CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);

        return await adminDb.SectorPackageCategories
            .Where(s => s.SectorPackageId == sectorPackageId)
            .Select(s => s.MasterCategoryId)
            .ToListAsync(ct);
    }

    // ── Yardımcı Metotlar ──────────────────────────────────────────────────

    /// <summary>
    /// Seçili kategori ID'lerini üst ağaç (kökten yaprağa) dahil ederek genişletir.
    /// Üst kategoriler parent link'ler için gerekli.
    /// </summary>
    private static async Task<HashSet<int>> ExpandCategoryIdsAsync(
        AdminPanelDbContext adminDb,
        IList<int> selectedIds,
        CancellationToken ct)
    {
        var result = new HashSet<int>(selectedIds);
        var allCategories = await adminDb.MasterCategories
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync(ct);

        var parentMap = allCategories.ToDictionary(c => c.Id, c => c.ParentId);

        // Seçilen her kategori için üst ağacı ekle
        foreach (var id in selectedIds.ToList())
        {
            var current = id;
            while (parentMap.TryGetValue(current, out var parentId) && parentId.HasValue)
            {
                result.Add(parentId.Value);
                current = parentId.Value;
            }
        }

        return result;
    }

    private static List<MasterCategoryTreeDto> BuildTree(
        List<MasterCategoryTreeDto> all,
        int? parentId)
    {
        return all
            .Where(c => c.ParentId == parentId)
            .Select(c =>
            {
                c.Children = BuildTree(all, c.Id);
                return c;
            })
            .OrderBy(c => c.SortOrder)
            .ToList();
    }
}
