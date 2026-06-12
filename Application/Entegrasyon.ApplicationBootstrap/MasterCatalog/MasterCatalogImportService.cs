using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.MasterCatalog;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.ApplicationBootstrap.MasterCatalog;

/// <summary>
/// Master catalog (AdminPanelDb) içeriğini tenant'ın IntegrationDb'sine aktarır.
/// Her iki DbContext'e erişim için IDbContextFactory kullanılır.
/// </summary>
public class MasterCatalogImportService(
    IDbContextFactory<AdminPanelDbContext> adminDbFactory,
    IDbContextFactory<IntegrationDbContext> integrationDbFactory,
    IMemoryCache cache,
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
                var existingCategoryId = await integrationDb.Categories
                    .Where(c => c.ExternalCategoryId == masterCat.OriginalExternalId)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(ct);

                Category? targetCategory = null;
                int targetCategoryId;
                bool isExisting = false;

                if (existingCategoryId.HasValue)
                {
                    targetCategoryId = existingCategoryId.Value;
                    categoryMap[masterCat.Id] = new Category { Id = targetCategoryId }; // sadece ID referansi
                    categoriesSkipped++;
                    isExisting = true;
                }
                else
                {
                    // Üst kategoriyi bul
                    Category? superCategory = null;
                    if (masterCat.ParentId.HasValue && categoryMap.TryGetValue(masterCat.ParentId.Value, out var parentCat))
                        superCategory = parentCat;

                    targetCategory = new Category
                    {
                        Name = masterCat.Name,
                        IsImported = true,
                        ImportSource = Entity.Categories.ImportSource.Trendyol,
                        ExternalCategoryId = masterCat.OriginalExternalId,
                        SuperCategory = superCategory
                    };

                    await integrationDb.Categories.AddAsync(targetCategory, ct);
                    categoryMap[masterCat.Id] = targetCategory;
                    categoriesImported++;
                    targetCategoryId = 0; // SaveChanges sonrasi atanacak, simdilik navigation kullanilir
                }

                // Marketplace eslestirme — hem yeni hem mevcut kategoriler icin calisir
                foreach (var mapping in masterCat.MarketplaceMappings)
                {
                    if (trendyolMarketPlace != null)
                    {
                        if (int.TryParse(mapping.ExternalCategoryId, out int extId))
                        {
                            // CategoryMarketplaces (yeni tablo) — dashboard ve sync bunu okur
                            bool mpExists = isExisting && await integrationDb.CategoryMarketplaces
                                .AnyAsync(cm => cm.CategoryId == targetCategoryId
                                             && cm.MarketPlaceId == trendyolMarketPlace.Id, ct);
                            if (!mpExists)
                            {
                                var newCm = new CategoryMarketplace
                                {
                                    MarketPlaceId = trendyolMarketPlace.Id,
                                    MarketPlaceCategoryId = extId,
                                    ExternalCategoryId = mapping.ExternalCategoryId,
                                    MarketPlaceCategoryName = mapping.ExternalCategoryName,
                                    IsActive = true
                                };
                                if (isExisting)
                                    newCm.CategoryId = targetCategoryId;
                                else
                                    newCm.Category = targetCategory!;
                                await integrationDb.CategoryMarketplaces.AddAsync(newCm, ct);
                            }

                            // CategoryMarketPlaceMatches (eski tablo) — uyumluluk
                            bool matchExists = isExisting && await integrationDb.CategoryMarketPlaceMatches
                                .AnyAsync(cm => cm.ApplicationCategoryId == targetCategoryId
                                             && cm.MarketPlaceId == trendyolMarketPlace.Id, ct);
                            if (!matchExists)
                            {
                                var newMatch = new CategoryMarketPlaceMatch
                                {
                                    MarketPlaceId = trendyolMarketPlace.Id,
                                    MarketPlaceCategoryId = extId
                                };
                                if (isExisting)
                                    newMatch.ApplicationCategoryId = targetCategoryId;
                                else
                                    newMatch.ApplicationCategory = targetCategory!;
                                await integrationDb.CategoryMarketPlaceMatches.AddAsync(newMatch, ct);
                            }

                            if (!mpExists || !matchExists)
                                mappingsImported++;
                        }
                    }
                }

                // Attribute'lar (sadece yeni yaprak kategoriler için)
                if (!isExisting && masterCat.IsLeaf)
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
                                .AsTracking()
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
                            .AnyAsync(j => j.Category == targetCategory && j.CategoryAttribute == categoryAttribute, ct);

                        if (!junctionExists)
                        {
                            await integrationDb.CategoryAttributeCategories.AddAsync(
                                new CategoryAttributeCategory
                                {
                                    Category = targetCategory!,
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

            // Kategori listesi cache'ini temizle
            cache.Remove("categories:list");

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

    public async Task<ImportResultDto> ImportBrandsFromMasterAsync(
        int tenantId,
        IList<int>? masterBrandIds = null,
        CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);
        await using var integrationDb = await integrationDbFactory.CreateDbContextAsync(ct);

        int brandsImported = 0, brandsSkipped = 0;

        var query = adminDb.MasterBrands
            .Include(b => b.MarketplaceMappings)
            .Where(b => b.IsActive);

        if (masterBrandIds is { Count: > 0 })
            query = query.Where(b => masterBrandIds.Contains(b.Id));

        var masterBrands = await query.OrderBy(b => b.Name).ToListAsync(ct);

        var trendyolMarketPlace = await integrationDb.MarketPlaces
            .AsTracking()
            .FirstOrDefaultAsync(m => m.Id == 1, ct);

        await using var transaction = await integrationDb.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var masterBrand in masterBrands)
            {
                var existingBrand = await integrationDb.Brands
                    .FirstOrDefaultAsync(b => b.Name == masterBrand.Name, ct);

                if (existingBrand != null)
                {
                    brandsSkipped++;
                    continue;
                }

                var newBrand = new Brand
                {
                    Name = masterBrand.Name
                };
                await integrationDb.Brands.AddAsync(newBrand, ct);
                brandsImported++;

                // Marketplace eşleştirmeleri
                foreach (var mapping in masterBrand.MarketplaceMappings)
                {
                    if (trendyolMarketPlace != null && mapping.MarketplaceId == trendyolMarketPlace.Id)
                    {
                        await integrationDb.BrandMarketPlaceMatches.AddAsync(new BrandMarketPlaceMatch
                        {
                            ApplicationBrand = newBrand,
                            MarketPlace = trendyolMarketPlace,
                            MarketPlaceBrandId = mapping.ExternalBrandId
                        }, ct);
                    }
                }
            }

            await integrationDb.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Marka import tamamlandı (tenantId={TenantId}): {Imported} eklendi, {Skipped} atlandı.",
                tenantId, brandsImported, brandsSkipped);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Marka import hatası (tenantId={TenantId}).", tenantId);
            throw;
        }

        return new ImportResultDto(0, 0, 0, 0, 0, 0, 0, brandsImported, brandsSkipped);
    }

    public async Task<IList<MasterBrandDto>> GetMasterBrandsAsync(CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);

        return await adminDb.MasterBrands
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new MasterBrandDto
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive,
                TrendyolBrandId = b.MarketplaceMappings
                    .Where(m => m.MarketplaceId == 1)
                    .Select(m => (int?)m.ExternalBrandId)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
    }

    public async Task<IList<MasterBrandDto>> SearchMasterBrandsAsync(
        string query, int limit = 50, CancellationToken ct = default)
    {
        await using var adminDb = await adminDbFactory.CreateDbContextAsync(ct);

        return await adminDb.MasterBrands
            .Where(b => b.IsActive && EF.Functions.ILike(b.Name, $"%{query}%"))
            .OrderBy(b => b.Name)
            .Take(limit)
            .Select(b => new MasterBrandDto
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive,
                TrendyolBrandId = b.MarketplaceMappings
                    .Where(m => m.MarketplaceId == 1)
                    .Select(m => (int?)m.ExternalBrandId)
                    .FirstOrDefault()
            })
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
