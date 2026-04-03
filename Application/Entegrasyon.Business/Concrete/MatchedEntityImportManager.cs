using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Templates;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Hazir eslesmis entity paketlerinin listelenmesi, cakisma tespiti ve import islemleri.
/// 3-step pipeline: Validation → Business Rules → Execution
/// </summary>
public class MatchedEntityImportManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<MatchedEntityImportManager> logger) : IMatchedEntityImportManager
{
    public async Task<IDataResult<Pageable<MatchedEntityPackageDto>>> GetAvailablePackagesAsync(
        MatchedEntityPackagePaginatedRequest request, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var query = dbContext.MatchedEntityPackages
            .Where(p => p.IsPublished)
            .AsQueryable();

        if (request.EntityType.HasValue)
            query = query.Where(p => p.EntityType == request.EntityType.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{request.SearchTerm}%"));

        var projected = query
            .OrderBy(p => p.Name)
            .Select(p => new MatchedEntityPackageDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                EntityType = p.EntityType,
                Version = p.Version,
                CreatedAt = p.CreatedAt,
                MarketplaceCount = p.EntityType == MatchedEntityType.Category
                    ? p.Categories.SelectMany(c => c.MarketplaceMappings).Select(m => m.MarketPlaceId).Distinct().Count()
                    : p.EntityType == MatchedEntityType.Brand
                        ? p.Brands.SelectMany(b => b.MarketplaceMappings).Select(m => m.MarketPlaceId).Distinct().Count()
                        : p.CargoCompanies.SelectMany(cc => cc.MarketplaceMappings).Select(m => m.MarketPlaceId).Distinct().Count(),
                EntityCount = p.EntityType == MatchedEntityType.Category
                    ? p.Categories.Count()
                    : p.EntityType == MatchedEntityType.Brand
                        ? p.Brands.Count()
                        : p.CargoCompanies.Count()
            });

        var totalCount = await projected.CountAsync(ct);
        if (totalCount == 0)
            return new SuccessDataResult<Pageable<MatchedEntityPackageDto>>(
                new Pageable<MatchedEntityPackageDto>([], request.PageIndex, request.PageSize, 0));

        var items = await projected
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new SuccessDataResult<Pageable<MatchedEntityPackageDto>>(
            new Pageable<MatchedEntityPackageDto>(items, request.PageIndex, request.PageSize, totalCount));
    }

    public async Task<IDataResult<MatchedEntityPackageDetailDto>> GetPackageDetailAsync(
        int packageId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var package = await dbContext.MatchedEntityPackages
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.Attributes)
                        .ThenInclude(a => a.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.Attributes)
                        .ThenInclude(a => a.Values)
                            .ThenInclude(v => v.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.MarketplaceMappings)
            .Include(p => p.Brands)
                .ThenInclude(b => b.MarketplaceMappings)
            .Include(p => p.CargoCompanies)
                .ThenInclude(cc => cc.MarketplaceMappings)
            .FirstOrDefaultAsync(p => p.Id == packageId, ct);

        if (package == null)
            return new ErrorDataResult<MatchedEntityPackageDetailDto>(default!, "Paket bulunamadı.");

        var dto = new MatchedEntityPackageDetailDto
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            EntityType = package.EntityType,
            Version = package.Version,
            CreatedAt = package.CreatedAt,
            Categories = package.Categories
                .Where(c => c.ParentTemplateCategoryDataId == null)
                .Select(MapCategoryToDto)
                .ToList(),
            Brands = package.Brands.Select(b => new TemplateBrandDetailDto
            {
                Id = b.Id,
                Name = b.Name,
                MarketplaceMappings = b.MarketplaceMappings.Select(m => new MarketplaceMappingDto
                {
                    MarketPlaceId = m.MarketPlaceId,
                    ExternalId = m.ExternalBrandId,
                    ExternalStringId = m.ExternalBrandExternalId
                }).ToList()
            }).ToList(),
            CargoCompanies = package.CargoCompanies.Select(cc => new TemplateCargoCompanyDetailDto
            {
                Id = cc.Id,
                Name = cc.Name,
                Code = cc.Code,
                MarketplaceMappings = cc.MarketplaceMappings.Select(m => new MarketplaceMappingDto
                {
                    MarketPlaceId = m.MarketPlaceId,
                    ExternalId = m.ExternalCargoCompanyId
                }).ToList()
            }).ToList()
        };

        return new SuccessDataResult<MatchedEntityPackageDetailDto>(dto);
    }

    public async Task<IDataResult<List<ImportConflictDto>>> DetectConflictsAsync(
        int packageId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var package = await dbContext.MatchedEntityPackages
            .Include(p => p.Categories)
                .ThenInclude(c => c.MarketplaceMappings)
            .Include(p => p.Brands)
                .ThenInclude(b => b.MarketplaceMappings)
            .Include(p => p.CargoCompanies)
                .ThenInclude(cc => cc.MarketplaceMappings)
            .FirstOrDefaultAsync(p => p.Id == packageId, ct);

        if (package == null)
            return new ErrorDataResult<List<ImportConflictDto>>([], "Paket bulunamadı.");

        var conflicts = new List<ImportConflictDto>();

        // Kategori cakismalari
        foreach (var templateCat in package.Categories)
        {
            var nameMatch = await dbContext.Categories
                .IgnoreQueryFilters()
                .Where(c => !c.IsDeleted)
                .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, templateCat.Name), ct);

            if (nameMatch != null)
            {
                conflicts.Add(new ImportConflictDto
                {
                    EntityType = "Category",
                    TemplateEntityId = templateCat.Id,
                    TemplateName = templateCat.Name,
                    ExistingEntityId = nameMatch.Id,
                    ExistingEntityName = nameMatch.Name,
                    ConflictType = ConflictType.NameMatch
                });
            }
        }

        // Brand cakismalari
        foreach (var templateBrand in package.Brands)
        {
            var nameMatch = await dbContext.Brands
                .FirstOrDefaultAsync(b => EF.Functions.ILike(b.Name, templateBrand.Name), ct);

            if (nameMatch != null)
            {
                conflicts.Add(new ImportConflictDto
                {
                    EntityType = "Brand",
                    TemplateEntityId = templateBrand.Id,
                    TemplateName = templateBrand.Name,
                    ExistingEntityId = nameMatch.Id,
                    ExistingEntityName = nameMatch.Name,
                    ConflictType = ConflictType.NameMatch
                });
            }
        }

        // CargoCompany cakismalari
        foreach (var templateCargo in package.CargoCompanies)
        {
            var nameMatch = await dbContext.CargoCompanies
                .FirstOrDefaultAsync(cc => EF.Functions.ILike(cc.Name, templateCargo.Name), ct);

            if (nameMatch != null)
            {
                conflicts.Add(new ImportConflictDto
                {
                    EntityType = "CargoCompany",
                    TemplateEntityId = templateCargo.Id,
                    TemplateName = templateCargo.Name,
                    ExistingEntityId = nameMatch.Id,
                    ExistingEntityName = nameMatch.Name,
                    ConflictType = ConflictType.NameMatch
                });
            }
        }

        return new SuccessDataResult<List<ImportConflictDto>>(conflicts);
    }

    public async Task<IResult> ImportPackageAsync(ImportPackageRequest request, CancellationToken ct = default)
    {
        // 1. Validation
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var package = await dbContext.MatchedEntityPackages
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.Attributes)
                        .ThenInclude(a => a.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
                    .ThenInclude(c => c.Attributes)
                        .ThenInclude(a => a.Values)
                            .ThenInclude(v => v.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Attributes)
                    .ThenInclude(a => a.MarketplaceMappings)
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Attributes)
                    .ThenInclude(a => a.Values)
                        .ThenInclude(v => v.MarketplaceMappings)
            .Include(p => p.Brands)
                .ThenInclude(b => b.MarketplaceMappings)
            .Include(p => p.CargoCompanies)
                .ThenInclude(cc => cc.MarketplaceMappings)
            .FirstOrDefaultAsync(p => p.Id == request.PackageId, ct);

        if (package == null)
            return new ErrorResult("Paket bulunamadı.");

        if (!package.IsPublished)
            return new ErrorResult("Bu paket henüz yayınlanmamış.");

        // 2. Business Rules — çakışma kontrolü resolution'suz yapılmış mı?
        // (Çakışmalar varsa ve resolution yoksa DetectConflicts'ten dönülmüş olmalı)

        // 3. Execution
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var resolutionMap = request.ConflictResolutions
                .ToDictionary(r => r.TemplateEntityId, r => r);

            switch (package.EntityType)
            {
                case MatchedEntityType.Category:
                    foreach (var rootCat in package.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                    {
                        await ImportCategoryRecursiveAsync(dbContext, rootCat, null, resolutionMap, ct);
                    }
                    break;

                case MatchedEntityType.Brand:
                    foreach (var brand in package.Brands)
                    {
                        await ImportBrandAsync(dbContext, brand, resolutionMap, ct);
                    }
                    break;

                case MatchedEntityType.CargoCompany:
                    foreach (var cargo in package.CargoCompanies)
                    {
                        await ImportCargoCompanyAsync(dbContext, cargo, resolutionMap, ct);
                    }
                    break;
            }

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation("MatchedEntity paketi basariyla import edildi: {PackageName} (Id={PackageId})",
                package.Name, package.Id);
            return new SuccessResult("Paket başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "MatchedEntity paketi import edilirken hata: {PackageName}", package.Name);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    // ─── Private Helpers ─────────────────────────────────────────────

    private async Task<Category> ImportCategoryRecursiveAsync(
        IntegrationDbContext dbContext,
        TemplateCategoryData templateCat,
        Category? parentCategory,
        Dictionary<int, ConflictResolution> resolutions,
        CancellationToken ct)
    {
        Category category;

        if (resolutions.TryGetValue(templateCat.Id, out var resolution))
        {
            // Cakisma var — strateji uygula
            var existing = await dbContext.Categories
                .AsTracking()
                .FirstAsync(c => c.Id == resolution.ExistingEntityId, ct);

            if (resolution.Strategy == ConflictResolutionStrategy.UseExisting)
            {
                // Mevcut entity'yi koru — alt kategorilere devam et
                category = existing;
            }
            else // UseImported
            {
                existing.Name = templateCat.Name;
                existing.DefaultVatRate = templateCat.DefaultVatRate;
                existing.SuperCategory = parentCategory;
                existing.ImportSource = ImportSource.MatchedEntity;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                category = existing;
            }
        }
        else
        {
            // Cakisma yok — yeni entity olustur
            category = new Category
            {
                Name = templateCat.Name,
                IsImported = true,
                ImportSource = ImportSource.MatchedEntity,
                SuperCategory = parentCategory,
                DefaultVatRate = templateCat.DefaultVatRate,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await dbContext.Categories.AddAsync(category, ct);
        }

        // Marketplace mapping'leri olustur
        foreach (var mapping in templateCat.MarketplaceMappings)
        {
            var existingMapping = await dbContext.CategoryMarketplaces
                .FirstOrDefaultAsync(cm =>
                    cm.CategoryId == category.Id && cm.MarketPlaceId == mapping.MarketPlaceId, ct);

            if (existingMapping == null)
            {
                dbContext.CategoryMarketplaces.Add(new CategoryMarketplace
                {
                    Category = category,
                    MarketPlaceId = mapping.MarketPlaceId,
                    MarketPlaceCategoryId = int.TryParse(mapping.ExternalCategoryId, out var intId) ? intId : 0,
                    ExternalCategoryId = mapping.ExternalCategoryId,
                    MarketPlaceCategoryName = mapping.ExternalCategoryName,
                    LastSyncedAt = DateTimeOffset.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        // Attribute'lari import et (leaf kategoriler icin veya tum kategoriler icin)
        foreach (var templateAttr in templateCat.Attributes)
        {
            await ImportAttributeAsync(dbContext, templateAttr, category, ct);
        }

        // Alt kategorileri isle
        foreach (var child in templateCat.Children)
        {
            await ImportCategoryRecursiveAsync(dbContext, child, category, resolutions, ct);
        }

        return category;
    }

    private async Task ImportAttributeAsync(
        IntegrationDbContext dbContext,
        TemplateCategoryAttributeData templateAttr,
        Category category,
        CancellationToken ct)
    {
        // Mevcut attribute'u bul veya olustur
        var existingAttr = await dbContext.CategoryAttributes
            .AsTracking()
            .FirstOrDefaultAsync(a =>
                EF.Functions.ILike(a.CategoryAttributeKey!, templateAttr.AttributeKey), ct);

        CategoryAttribute attr;
        if (existingAttr != null)
        {
            attr = existingAttr;
        }
        else
        {
            attr = new CategoryAttribute
            {
                CategoryAttributeKey = templateAttr.AttributeKey,
                CategoryAttributeHumanized = templateAttr.AttributeHumanized,
                AllowCustom = templateAttr.AllowCustom,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await dbContext.CategoryAttributes.AddAsync(attr, ct);
        }

        // Junction: CategoryAttributeCategory
        await dbContext.SaveChangesAsync(ct); // attr.Id gerekli
        var junctionExists = await dbContext.CategoryAttributeCategories
            .AnyAsync(cac => cac.CategoryId == category.Id && cac.CategoryAttributeId == attr.Id, ct);

        if (!junctionExists)
        {
            dbContext.CategoryAttributeCategories.Add(new CategoryAttributeCategory
            {
                Category = category,
                CategoryAttribute = attr,
                IsRequired = templateAttr.IsRequired,
                IsSlicer = templateAttr.IsSlicer,
                IsVarianter = templateAttr.IsVarianter,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Attribute marketplace mapping'ler
        foreach (var attrMapping in templateAttr.MarketplaceMappings)
        {
            var existsAttrMatch = await dbContext.CategoryAttributeMarketPlaceMatches
                .AnyAsync(m => m.ApplicationCategoryAttributeId == attr.Id && m.MarketPlaceId == attrMapping.MarketPlaceId, ct);

            if (!existsAttrMatch)
            {
                dbContext.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
                {
                    ApplicationCategoryAttribute = attr,
                    MarketPlaceId = attrMapping.MarketPlaceId,
                    MarketPlaceCategoryAttributeId = attrMapping.ExternalAttributeId,
                    MarketPlaceCategoryAttributeExternalId = attrMapping.ExternalAttributeExternalId
                });
            }
        }

        // Attribute value'lari
        foreach (var templateValue in templateAttr.Values)
        {
            var existingValue = await dbContext.CategoryAttributeValues
                .FirstOrDefaultAsync(v =>
                    v.CategoryAttributeId == attr.Id &&
                    EF.Functions.ILike(v.Name!, templateValue.ValueName), ct);

            CategoryAttributeValue value;
            if (existingValue != null)
            {
                value = existingValue;
            }
            else
            {
                value = new CategoryAttributeValue
                {
                    CategoryAttribute = attr,
                    Name = templateValue.ValueName,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await dbContext.CategoryAttributeValues.AddAsync(value, ct);
                await dbContext.SaveChangesAsync(ct); // value.Id gerekli
            }

            // Value marketplace mapping'ler
            foreach (var valueMapping in templateValue.MarketplaceMappings)
            {
                var existsValueMatch = await dbContext.CategoryAttributeValueMarketPlaceMatches
                    .AnyAsync(m =>
                        m.ApplicationCategoryAttributeValueId == value.Id &&
                        m.MarketPlaceId == valueMapping.MarketPlaceId, ct);

                if (!existsValueMatch)
                {
                    dbContext.CategoryAttributeValueMarketPlaceMatches.Add(
                        new CategoryAttributeValueMarketPlaceMatch
                        {
                            ApplicationCategoryAttributeValue = value,
                            MarketPlaceId = valueMapping.MarketPlaceId,
                            MarketPlaceCategoryAttributeValueId = valueMapping.ExternalValueId,
                            MarketPlaceCategoryAttributeValueExternalId = valueMapping.ExternalValueExternalId
                        });
                }
            }
        }
    }

    private async Task ImportBrandAsync(
        IntegrationDbContext dbContext,
        TemplateBrandData templateBrand,
        Dictionary<int, ConflictResolution> resolutions,
        CancellationToken ct)
    {
        Entity.Brands.Brand brand;

        if (resolutions.TryGetValue(templateBrand.Id, out var resolution))
        {
            var existing = await dbContext.Brands
                .AsTracking()
                .FirstAsync(b => b.Id == resolution.ExistingEntityId, ct);

            if (resolution.Strategy == ConflictResolutionStrategy.UseExisting)
                brand = existing;
            else
            {
                existing.Name = templateBrand.Name;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                brand = existing;
            }
        }
        else
        {
            brand = new Entity.Brands.Brand
            {
                Name = templateBrand.Name,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await dbContext.Brands.AddAsync(brand, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        foreach (var mapping in templateBrand.MarketplaceMappings)
        {
            var exists = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.ApplicationBrandId == brand.Id && m.MarketPlaceId == mapping.MarketPlaceId, ct);

            if (!exists)
            {
                dbContext.BrandMarketPlaceMatches.Add(new BrandMarketPlaceMatch
                {
                    ApplicationBrand = brand,
                    MarketPlaceId = mapping.MarketPlaceId,
                    MarketPlaceBrandId = mapping.ExternalBrandId,
                    MarketPlaceBrandExternalId = mapping.ExternalBrandExternalId
                });
            }
        }
    }

    private async Task ImportCargoCompanyAsync(
        IntegrationDbContext dbContext,
        TemplateCargoCompanyData templateCargo,
        Dictionary<int, ConflictResolution> resolutions,
        CancellationToken ct)
    {
        Entity.CargoCompany cargo;

        if (resolutions.TryGetValue(templateCargo.Id, out var resolution))
        {
            var existing = await dbContext.CargoCompanies
                .AsTracking()
                .FirstAsync(cc => cc.Id == resolution.ExistingEntityId, ct);

            if (resolution.Strategy == ConflictResolutionStrategy.UseExisting)
                cargo = existing;
            else
            {
                existing.Name = templateCargo.Name;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                cargo = existing;
            }
        }
        else
        {
            cargo = new Entity.CargoCompany
            {
                Name = templateCargo.Name,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await dbContext.CargoCompanies.AddAsync(cargo, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        foreach (var mapping in templateCargo.MarketplaceMappings)
        {
            var exists = await dbContext.CargoCompanyMarketPlaceMatches
                .AnyAsync(m => m.ApplicationCargoCompanyId == cargo.Id && m.MarketPlaceId == mapping.MarketPlaceId, ct);

            if (!exists)
            {
                dbContext.CargoCompanyMarketPlaceMatches.Add(new CargoCompanyMarketPlaceMatch
                {
                    ApplicationCargoCompany = cargo,
                    MarketPlaceId = mapping.MarketPlaceId,
                    MarketPlaceCargoCompanyId = mapping.ExternalCargoCompanyId
                });
            }
        }
    }

    private static TemplateCategoryDetailDto MapCategoryToDto(TemplateCategoryData cat) => new()
    {
        Id = cat.Id,
        Name = cat.Name,
        DefaultVatRate = cat.DefaultVatRate,
        Children = cat.Children.OrderBy(c => c.SortOrder).Select(MapCategoryToDto).ToList(),
        MarketplaceMappings = cat.MarketplaceMappings.Select(m => new MarketplaceMappingDto
        {
            MarketPlaceId = m.MarketPlaceId,
            ExternalId = int.TryParse(m.ExternalCategoryId, out var id) ? id : 0,
            ExternalStringId = m.ExternalCategoryId,
            ExternalName = m.ExternalCategoryName
        }).ToList(),
        Attributes = cat.Attributes.Select(a => new TemplateAttributeDetailDto
        {
            Id = a.Id,
            AttributeKey = a.AttributeKey,
            AttributeHumanized = a.AttributeHumanized,
            AllowCustom = a.AllowCustom,
            IsRequired = a.IsRequired,
            IsSlicer = a.IsSlicer,
            IsVarianter = a.IsVarianter,
            MarketplaceMappings = a.MarketplaceMappings.Select(m => new MarketplaceMappingDto
            {
                MarketPlaceId = m.MarketPlaceId,
                ExternalId = m.ExternalAttributeId,
                ExternalStringId = m.ExternalAttributeExternalId
            }).ToList(),
            Values = a.Values.Select(v => new TemplateAttributeValueDetailDto
            {
                Id = v.Id,
                ValueName = v.ValueName,
                MarketplaceMappings = v.MarketplaceMappings.Select(m => new MarketplaceMappingDto
                {
                    MarketPlaceId = m.MarketPlaceId,
                    ExternalId = m.ExternalValueId,
                    ExternalStringId = m.ExternalValueExternalId
                }).ToList()
            }).ToList()
        }).ToList()
    };
}
