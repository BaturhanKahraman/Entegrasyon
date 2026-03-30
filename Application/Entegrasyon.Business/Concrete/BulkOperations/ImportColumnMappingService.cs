using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public class ImportColumnMappingService(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IImportColumnMappingService
{
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Barkod"] = ["Barcode", "Bar Code", "EAN", "UPC", "SKU", "Barcod"],
        ["Ürün Adı"] = ["Product Name", "Title", "Ad", "Ürün Başlığı", "Ürün", "Urun Adi", "UrunAdi"],
        ["Stok Kodu"] = ["Stock Code", "SKU Code", "Stok No", "StokKodu"],
        ["Liste Fiyatı"] = ["List Price", "Price", "Fiyat", "MSRP", "ListeFiyati"],
        ["Satış Fiyatı"] = ["Sale Price", "Selling Price", "İndirimli Fiyat", "SatisFiyati"],
        ["Maliyet Fiyatı"] = ["Cost Price", "Cost", "Maliyet", "MaliyetFiyati"],
        ["KDV Oranı"] = ["VAT Rate", "Tax Rate", "KDV", "Vergi", "KdvOrani"],
        ["Kategori"] = ["Category", "Kategori Adı", "Category Name", "KategoriAdi"],
        ["Marka"] = ["Brand", "Brand Name", "Marka Adı", "MarkaAdi"],
        ["Şube ID"] = ["Branch ID", "Branch Office ID", "Depo ID", "Warehouse ID", "SubeId"],
        ["Stok Miktarı"] = ["Stock Quantity", "Quantity", "Adet", "Miktar", "StokMiktari"]
    };

    private static readonly Dictionary<BulkOperationType, List<ImportSystemField>> SystemFields = new()
    {
        [BulkOperationType.ProductImport] =
        [
            new("Barkod", "Barkod", true),
            new("Ürün Adı", "Ürün Adı", true),
            new("Stok Kodu", "Stok Kodu", false),
            new("Liste Fiyatı", "Liste Fiyatı", true),
            new("Satış Fiyatı", "Satış Fiyatı", true),
            new("Maliyet Fiyatı", "Maliyet Fiyatı", false),
            new("KDV Oranı", "KDV Oranı", false),
            new("Kategori", "Kategori", false),
            new("Marka", "Marka", false)
        ],
        [BulkOperationType.PriceImport] =
        [
            new("Barkod", "Barkod", true),
            new("Liste Fiyatı", "Liste Fiyatı", true),
            new("Satış Fiyatı", "Satış Fiyatı", true),
            new("Maliyet Fiyatı", "Maliyet Fiyatı", false)
        ],
        [BulkOperationType.StockImport] =
        [
            new("Barkod", "Barkod", true),
            new("Şube ID", "Şube ID", true),
            new("Stok Miktarı", "Stok Miktarı", true)
        ]
    };

    public async Task<List<ImportColumnProfile>> GetProfilesAsync(BulkOperationType type)
    {
        using var db = contextFactory.CreateDbContext();
        return await db.ImportColumnProfiles
            .AsNoTracking()
            .Where(p => p.ImportType == type && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IResult> SaveProfileAsync(string name, BulkOperationType type, Dictionary<string, string> mappings)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ErrorResult("Profil adı boş olamaz.");

        using var db = contextFactory.CreateDbContext();

        var exists = await db.ImportColumnProfiles
            .AnyAsync(p => p.Name == name.Trim() && p.ImportType == type && !p.IsDeleted);
        if (exists)
            return new ErrorResult("Bu isimde bir profil zaten mevcut.");

        db.ImportColumnProfiles.Add(new ImportColumnProfile
        {
            Name = name.Trim(),
            ImportType = type,
            MappingsJson = JsonSerializer.Serialize(mappings)
        });

        await db.SaveChangesAsync();
        return new SuccessResult("Profil kaydedildi.");
    }

    public async Task<IResult> DeleteProfileAsync(int id)
    {
        using var db = contextFactory.CreateDbContext();
        var profile = await db.ImportColumnProfiles.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null)
            return new ErrorResult("Profil bulunamadı.");

        profile.IsDeleted = true;
        await db.SaveChangesAsync();
        return new SuccessResult("Profil silindi.");
    }

    public Dictionary<string, string?> SuggestMappings(List<string> excelHeaders, BulkOperationType type)
    {
        var fields = GetSystemFields(type);
        var result = new Dictionary<string, string?>();

        foreach (var field in fields)
        {
            result[field.Key] = FindBestMatch(field.Key, excelHeaders);
        }

        return result;
    }

    public List<ImportSystemField> GetSystemFields(BulkOperationType type)
    {
        return SystemFields.TryGetValue(type, out var fields) ? fields : [];
    }

    private static string? FindBestMatch(string systemField, List<string> excelHeaders)
    {
        // 1. Exact match (case-insensitive, trim)
        var exact = excelHeaders.FirstOrDefault(h =>
            h.Trim().Equals(systemField, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // 2. Normalized match (Turkish chars)
        var normalizedSystem = TurkishStringNormalizer.Normalize(systemField);
        var normalized = excelHeaders.FirstOrDefault(h =>
            TurkishStringNormalizer.Normalize(h.Trim())
                .Equals(normalizedSystem, StringComparison.OrdinalIgnoreCase));
        if (normalized is not null) return normalized;

        // 3. Contains match
        var contains = excelHeaders.FirstOrDefault(h =>
            h.Trim().Contains(systemField, StringComparison.OrdinalIgnoreCase) ||
            systemField.Contains(h.Trim(), StringComparison.OrdinalIgnoreCase));
        if (contains is not null) return contains;

        // 4. Alias lookup
        if (Aliases.TryGetValue(systemField, out var aliases))
        {
            foreach (var alias in aliases)
            {
                var aliasMatch = excelHeaders.FirstOrDefault(h =>
                    h.Trim().Equals(alias, StringComparison.OrdinalIgnoreCase));
                if (aliasMatch is not null) return aliasMatch;

                // Normalized alias
                var normalizedAlias = TurkishStringNormalizer.Normalize(alias);
                var normalizedAliasMatch = excelHeaders.FirstOrDefault(h =>
                    TurkishStringNormalizer.Normalize(h.Trim())
                        .Equals(normalizedAlias, StringComparison.OrdinalIgnoreCase));
                if (normalizedAliasMatch is not null) return normalizedAliasMatch;
            }
        }

        return null;
    }
}
