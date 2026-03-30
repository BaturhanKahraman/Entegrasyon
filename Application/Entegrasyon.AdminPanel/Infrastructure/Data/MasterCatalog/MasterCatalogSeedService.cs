using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

/// <summary>
/// AdminPanelDb'ye master catalog verisi yükler.
/// Gerçek Trendyol snapshot varsa ondan okur; yoksa mock veri kullanır.
/// </summary>
public class MasterCatalogSeedService(
    AdminPanelDbContext dbContext,
    ILogger<MasterCatalogSeedService> logger)
{
    private const int TrendyolMarketplaceId = 1;
    private const string TrendyolBrandApiUrl = "https://apigw.trendyol.com/integration/product/brands";

    public async Task SeedAsync(string? snapshotFilePath = null)
    {
        if (await dbContext.MasterCategories.AnyAsync())
        {
            logger.LogInformation("Master catalog zaten yüklü, seed atlanıyor.");
            return;
        }

        var snapshot = LoadSnapshot(snapshotFilePath);
        await SeedFromSnapshotAsync(snapshot);
        await SeedSectorPackagesAsync();

        logger.LogInformation(
            "Master catalog seed tamamlandı: {Categories} kategori, {Attributes} attribute, {Values} değer.",
            await dbContext.MasterCategories.CountAsync(),
            await dbContext.MasterAttributes.CountAsync(),
            await dbContext.MasterAttributeValues.CountAsync());
    }

    private CatalogSnapshot LoadSnapshot(string? filePath)
    {
        if (filePath != null && File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<CatalogSnapshot>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? BuildMockSnapshot();
        }

        logger.LogInformation("Trendyol snapshot bulunamadı, mock veri kullanılıyor.");
        return BuildMockSnapshot();
    }

    private async Task SeedFromSnapshotAsync(CatalogSnapshot snapshot)
    {
        // Attribute deduplication: ExternalId → MasterAttribute
        var attributeCache = new Dictionary<string, MasterAttribute>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var cat in snapshot.Categories)
                await ProcessCategoryAsync(cat, null, attributeCache);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ProcessCategoryAsync(
        SnapshotCategory cat,
        MasterCategory? parent,
        Dictionary<string, MasterAttribute> attributeCache)
    {
        bool isLeaf = cat.SubCategories is null || cat.SubCategories.Count == 0;

        var masterCategory = new MasterCategory
        {
            Name = cat.Name,
            Parent = parent,
            SortOrder = cat.SortOrder,
            IsActive = true,
            IsLeaf = isLeaf,
            OriginalMarketplaceId = TrendyolMarketplaceId,
            OriginalExternalId = cat.Id.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await dbContext.MasterCategories.AddAsync(masterCategory);

        // Marketplace mapping
        await dbContext.MasterCategoryMarketplaceMappings.AddAsync(new MasterCategoryMarketplaceMapping
        {
            MasterCategory = masterCategory,
            MarketplaceId = TrendyolMarketplaceId,
            ExternalCategoryId = cat.Id.ToString(),
            ExternalCategoryName = cat.Name
        });

        // MarketplaceReference kaydı
        await dbContext.MarketplaceReferences.AddAsync(new MarketplaceReference
        {
            MarketplaceId = TrendyolMarketplaceId,
            EntityType = MarketplaceEntityType.Category,
            ExternalId = cat.Id.ToString(),
            Name = cat.Name,
            ParentExternalId = cat.ParentId?.ToString(),
            LastSyncedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Attribute'lar (yaprak kategoriler için)
        if (isLeaf && cat.Attributes is { Count: > 0 })
        {
            foreach (var attr in cat.Attributes)
            {
                var masterAttr = await GetOrCreateAttributeAsync(attr, attributeCache);

                // Junction kaydı
                await dbContext.MasterCategoryAttributes.AddAsync(new MasterCategoryAttribute
                {
                    MasterCategory = masterCategory,
                    MasterAttribute = masterAttr,
                    IsRequired = attr.Required,
                    IsVarianter = attr.Varianter,
                    IsSlicer = attr.Slicer
                });
            }
        }

        // Alt kategorileri recursive işle
        if (cat.SubCategories is { Count: > 0 })
        {
            foreach (var sub in cat.SubCategories)
                await ProcessCategoryAsync(sub, masterCategory, attributeCache);
        }
    }

    private async Task<MasterAttribute> GetOrCreateAttributeAsync(
        SnapshotAttribute attr,
        Dictionary<string, MasterAttribute> cache)
    {
        string cacheKey = attr.AttributeId.ToString();
        if (cache.TryGetValue(cacheKey, out var existing))
            return existing;

        var masterAttr = new MasterAttribute
        {
            Key = attr.AttributeName,
            HumanizedName = attr.AttributeName,
            AllowCustom = attr.AllowCustom,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await dbContext.MasterAttributes.AddAsync(masterAttr);

        // Marketplace mapping
        await dbContext.MasterAttributeMarketplaceMappings.AddAsync(new MasterAttributeMarketplaceMapping
        {
            MasterAttribute = masterAttr,
            MarketplaceId = TrendyolMarketplaceId,
            ExternalAttributeId = attr.AttributeId.ToString(),
            ExternalAttributeName = attr.AttributeName
        });

        // MarketplaceReference
        await dbContext.MarketplaceReferences.AddAsync(new MarketplaceReference
        {
            MarketplaceId = TrendyolMarketplaceId,
            EntityType = MarketplaceEntityType.Attribute,
            ExternalId = attr.AttributeId.ToString(),
            Name = attr.AttributeName,
            LastSyncedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Değerler
        if (attr.Values is { Count: > 0 })
        {
            foreach (var val in attr.Values)
            {
                var masterVal = new MasterAttributeValue
                {
                    MasterAttribute = masterAttr,
                    Name = val.Name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await dbContext.MasterAttributeValues.AddAsync(masterVal);

                await dbContext.MasterValueMarketplaceMappings.AddAsync(new MasterValueMarketplaceMapping
                {
                    MasterAttributeValue = masterVal,
                    MarketplaceId = TrendyolMarketplaceId,
                    ExternalValueId = val.Id.ToString(),
                    ExternalValueName = val.Name
                });
            }
        }

        cache[cacheKey] = masterAttr;
        return masterAttr;
    }

    /// <summary>
    /// Trendyol'dan tüm markaları sayfalı olarak çekip MasterBrand tablosuna yükler.
    /// Zaten veri varsa atlanır.
    /// </summary>
    public async Task SeedBrandsAsync()
    {
        if (await dbContext.MasterBrands.AnyAsync())
        {
            logger.LogInformation("Master marka verisi zaten yüklü, seed atlanıyor.");
            return;
        }

        logger.LogInformation("Trendyol marka verisi çekiliyor...");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Entegrasyon/1.0");

        var allBrands = new List<TrendyolBrandDto>();
        int page = 0;
        const int pageSize = 500;

        try
        {
            while (true)
            {
                var url = $"{TrendyolBrandApiUrl}?page={page}&size={pageSize}";
                var response = await httpClient.GetFromJsonAsync<TrendyolBrandPageResponse>(url,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response?.Brands is not { Count: > 0 })
                    break;

                allBrands.AddRange(response.Brands);
                logger.LogInformation("Sayfa {Page}: {Count} marka alındı (toplam: {Total})", page, response.Brands.Count, allBrands.Count);

                if (response.Brands.Count < pageSize)
                    break;

                page++;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Trendyol marka API'sinden veri çekilemedi, mock marka verisi kullanılıyor.");
            allBrands = BuildMockBrands();
        }

        if (allBrands.Count == 0)
        {
            logger.LogWarning("Hiç marka verisi alınamadı.");
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // Duplicate isimleri normalize ederek tekil tut
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var brandsToInsert = new List<(TrendyolBrandDto Dto, MasterBrand Entity)>();

            foreach (var brandDto in allBrands)
            {
                if (string.IsNullOrWhiteSpace(brandDto.Name))
                    continue;

                var normalizedName = brandDto.Name.Trim();
                if (!seenNames.Add(normalizedName))
                    continue;

                var masterBrand = new MasterBrand
                {
                    Name = normalizedName,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                brandsToInsert.Add((brandDto, masterBrand));
            }

            await dbContext.MasterBrands.AddRangeAsync(brandsToInsert.Select(b => b.Entity));

            // Mapping'leri SaveChanges sonrasına bırakmak yerine entity ref üzerinden ekle
            var mappings = brandsToInsert.Select(b => new MasterBrandMarketplaceMapping
            {
                MasterBrand = b.Entity,
                MarketplaceId = TrendyolMarketplaceId,
                ExternalBrandId = b.Dto.Id,
                ExternalBrandName = b.Dto.Name
            }).ToList();

            await dbContext.MasterBrandMarketplaceMappings.AddRangeAsync(mappings);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Marka seed tamamlandı: {Count} marka eklendi.", brandsToInsert.Count);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static List<TrendyolBrandDto> BuildMockBrands() =>
    [
        new TrendyolBrandDto { Id = 1, Name = "Nike" },
        new TrendyolBrandDto { Id = 2, Name = "Adidas" },
        new TrendyolBrandDto { Id = 3, Name = "Samsung" },
        new TrendyolBrandDto { Id = 4, Name = "Apple" },
        new TrendyolBrandDto { Id = 5, Name = "Xiaomi" }
    ];

    private async Task SeedSectorPackagesAsync()
    {
        if (await dbContext.SectorPackages.AnyAsync())
            return;

        var packages = new[]
        {
            new SectorPackage
            {
                Name = "Giyim & Moda",
                Description = "Kadın, erkek ve çocuk giyim kategorileri",
                IconName = "Icons.Material.Filled.Checkroom",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SectorPackage
            {
                Name = "Elektronik",
                Description = "Telefon, bilgisayar ve beyaz eşya kategorileri",
                IconName = "Icons.Material.Filled.Devices",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SectorPackage
            {
                Name = "Kozmetik & Bakım",
                Description = "Cilt bakımı, makyaj ve parfüm kategorileri",
                IconName = "Icons.Material.Filled.Spa",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SectorPackage
            {
                Name = "Ev & Yaşam",
                Description = "Mobilya, dekor ve mutfak kategorileri",
                IconName = "Icons.Material.Filled.Home",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SectorPackage
            {
                Name = "Spor & Outdoor",
                Description = "Spor malzemeleri ve kamp kategorileri",
                IconName = "Icons.Material.Filled.SportsSoccer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await dbContext.SectorPackages.AddRangeAsync(packages);

        // Sektör paketlerini mevcut master kategorilerle ilişkilendir
        var allLeafCategories = await dbContext.MasterCategories
            .Where(c => c.IsLeaf && c.IsActive)
            .ToListAsync();

        // Basit keyword eşleştirme ile paket-kategori bağlantısı
        var packageKeywords = new Dictionary<string, string[]>
        {
            ["Giyim & Moda"] = ["giyim", "tekstil", "konfeksiyon", "elbise", "mont", "ceket", "pantolon", "tişört", "gömlek"],
            ["Elektronik"] = ["elektronik", "bilgisayar", "telefon", "tablet", "kamera", "televizyon", "beyaz eşya"],
            ["Kozmetik & Bakım"] = ["kozmetik", "makyaj", "parfüm", "cilt", "saç", "kişisel bakım"],
            ["Ev & Yaşam"] = ["ev", "mobilya", "dekor", "mutfak", "banyo", "yatak", "salon"],
            ["Spor & Outdoor"] = ["spor", "fitness", "outdoor", "kamp", "bisiklet", "koşu"]
        };

        foreach (var package in packages)
        {
            if (!packageKeywords.TryGetValue(package.Name, out var keywords)) continue;

            var matchedCategories = allLeafCategories
                .Where(c => keywords.Any(k => c.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Take(20)
                .ToList();

            foreach (var cat in matchedCategories)
            {
                await dbContext.SectorPackageCategories.AddAsync(new SectorPackageCategory
                {
                    SectorPackage = package,
                    MasterCategory = cat
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    // ── Mock veri ──────────────────────────────────────────────────────────

    private static CatalogSnapshot BuildMockSnapshot() => new()
    {
        SnapshotDate = "2026-03-30",
        Categories =
        [
            new SnapshotCategory
            {
                Id = 1,
                Name = "Giyim",
                ParentId = null,
                SortOrder = 1,
                SubCategories =
                [
                    new SnapshotCategory
                    {
                        Id = 2,
                        Name = "Kadın Giyim",
                        ParentId = 1,
                        SortOrder = 1,
                        SubCategories =
                        [
                            new SnapshotCategory
                            {
                                Id = 11,
                                Name = "Kadın Elbise",
                                ParentId = 2,
                                SortOrder = 1,
                                Attributes =
                                [
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 101,
                                        AttributeName = "Renk",
                                        Required = true,
                                        Varianter = true,
                                        Slicer = true,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 1001, Name = "Siyah" },
                                            new SnapshotAttributeValue { Id = 1002, Name = "Beyaz" },
                                            new SnapshotAttributeValue { Id = 1003, Name = "Kırmızı" },
                                            new SnapshotAttributeValue { Id = 1004, Name = "Mavi" }
                                        ]
                                    },
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 102,
                                        AttributeName = "Beden",
                                        Required = true,
                                        Varianter = true,
                                        Slicer = false,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 2001, Name = "XS" },
                                            new SnapshotAttributeValue { Id = 2002, Name = "S" },
                                            new SnapshotAttributeValue { Id = 2003, Name = "M" },
                                            new SnapshotAttributeValue { Id = 2004, Name = "L" },
                                            new SnapshotAttributeValue { Id = 2005, Name = "XL" }
                                        ]
                                    },
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 103,
                                        AttributeName = "Materyal",
                                        Required = false,
                                        Varianter = false,
                                        Slicer = false,
                                        AllowCustom = true,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 3001, Name = "Pamuk" },
                                            new SnapshotAttributeValue { Id = 3002, Name = "Polyester" },
                                            new SnapshotAttributeValue { Id = 3003, Name = "Viskon" }
                                        ]
                                    }
                                ]
                            },
                            new SnapshotCategory
                            {
                                Id = 12,
                                Name = "Kadın Pantolon",
                                ParentId = 2,
                                SortOrder = 2,
                                Attributes =
                                [
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 101,
                                        AttributeName = "Renk",
                                        Required = true,
                                        Varianter = true,
                                        Slicer = true,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 1001, Name = "Siyah" },
                                            new SnapshotAttributeValue { Id = 1002, Name = "Beyaz" },
                                            new SnapshotAttributeValue { Id = 1003, Name = "Kırmızı" },
                                            new SnapshotAttributeValue { Id = 1004, Name = "Mavi" }
                                        ]
                                    },
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 102,
                                        AttributeName = "Beden",
                                        Required = true,
                                        Varianter = true,
                                        Slicer = false,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 2001, Name = "XS" },
                                            new SnapshotAttributeValue { Id = 2002, Name = "S" },
                                            new SnapshotAttributeValue { Id = 2003, Name = "M" },
                                            new SnapshotAttributeValue { Id = 2004, Name = "L" },
                                            new SnapshotAttributeValue { Id = 2005, Name = "XL" }
                                        ]
                                    }
                                ]
                            }
                        ]
                    },
                    new SnapshotCategory
                    {
                        Id = 3,
                        Name = "Erkek Giyim",
                        ParentId = 1,
                        SortOrder = 2,
                        SubCategories =
                        [
                            new SnapshotCategory
                            {
                                Id = 13,
                                Name = "Erkek Tişört",
                                ParentId = 3,
                                SortOrder = 1,
                                Attributes =
                                [
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 101,
                                        AttributeName = "Renk",
                                        Required = true,
                                        Varianter = true,
                                        Slicer = true,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 1001, Name = "Siyah" },
                                            new SnapshotAttributeValue { Id = 1002, Name = "Beyaz" },
                                            new SnapshotAttributeValue { Id = 1003, Name = "Kırmızı" },
                                            new SnapshotAttributeValue { Id = 1004, Name = "Mavi" }
                                        ]
                                    },
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 104,
                                        AttributeName = "Erkek Beden",
                                        Required = true,
                                        Varianter = true,
                                        Slicer = false,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 4001, Name = "S" },
                                            new SnapshotAttributeValue { Id = 4002, Name = "M" },
                                            new SnapshotAttributeValue { Id = 4003, Name = "L" },
                                            new SnapshotAttributeValue { Id = 4004, Name = "XL" },
                                            new SnapshotAttributeValue { Id = 4005, Name = "XXL" }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            new SnapshotCategory
            {
                Id = 4,
                Name = "Elektronik",
                ParentId = null,
                SortOrder = 2,
                SubCategories =
                [
                    new SnapshotCategory
                    {
                        Id = 5,
                        Name = "Cep Telefonu & Aksesuarlar",
                        ParentId = 4,
                        SortOrder = 1,
                        SubCategories =
                        [
                            new SnapshotCategory
                            {
                                Id = 21,
                                Name = "Cep Telefonu",
                                ParentId = 5,
                                SortOrder = 1,
                                Attributes =
                                [
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 201,
                                        AttributeName = "Marka",
                                        Required = true,
                                        Varianter = false,
                                        Slicer = false,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 5001, Name = "Apple" },
                                            new SnapshotAttributeValue { Id = 5002, Name = "Samsung" },
                                            new SnapshotAttributeValue { Id = 5003, Name = "Xiaomi" },
                                            new SnapshotAttributeValue { Id = 5004, Name = "Huawei" }
                                        ]
                                    },
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 202,
                                        AttributeName = "İşletim Sistemi",
                                        Required = true,
                                        Varianter = false,
                                        Slicer = false,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 6001, Name = "iOS" },
                                            new SnapshotAttributeValue { Id = 6002, Name = "Android" }
                                        ]
                                    },
                                    new SnapshotAttribute
                                    {
                                        AttributeId = 203,
                                        AttributeName = "Dahili Bellek",
                                        Required = false,
                                        Varianter = true,
                                        Slicer = false,
                                        AllowCustom = false,
                                        Values =
                                        [
                                            new SnapshotAttributeValue { Id = 7001, Name = "64 GB" },
                                            new SnapshotAttributeValue { Id = 7002, Name = "128 GB" },
                                            new SnapshotAttributeValue { Id = 7003, Name = "256 GB" },
                                            new SnapshotAttributeValue { Id = 7004, Name = "512 GB" }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        ]
    };
}

// ── Snapshot DTO'ları ──────────────────────────────────────────────────────

public class CatalogSnapshot
{
    public string SnapshotDate { get; set; } = string.Empty;
    public List<SnapshotCategory> Categories { get; set; } = [];
}

public class SnapshotCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public List<SnapshotCategory>? SubCategories { get; set; }
    public List<SnapshotAttribute>? Attributes { get; set; }
}

public class SnapshotAttribute
{
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool Varianter { get; set; }
    public bool Slicer { get; set; }
    public bool AllowCustom { get; set; }
    public List<SnapshotAttributeValue>? Values { get; set; }
}

public class SnapshotAttributeValue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ── Trendyol Brand API DTO'ları ────────────────────────────────────────────

public class TrendyolBrandPageResponse
{
    [JsonPropertyName("brands")]
    public List<TrendyolBrandDto> Brands { get; set; } = [];
}

public class TrendyolBrandDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
