using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Ic urun verisini PttAVM JSON formatina donusturur.
/// Varyantli urunlerde bos varyant dizisi gonderilmemelidir (PttAVM varyant siler).
/// </summary>
public sealed class PttavmProductMapper(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<PttavmProductMapper> logger) : IPttavmProductMapper
{
    public async Task<IDataResult<List<PttavmProductRequest>>> MapToUpsertRequestAsync(
        Guid productId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
            return new ErrorDataResult<List<PttavmProductRequest>>(null, "Ürün bulunamadı.");

        // Kategori eslestirmesi
        var categoryMatch = await dbContext.CategoryMarketplaces
            .FirstOrDefaultAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == PttavmMarketPlaceId, ct);

        if (categoryMatch is null)
            return new ErrorDataResult<List<PttavmProductRequest>>(null, "Kategori PttAVM'ye eşleştirilmemiş.");

        var requests = new List<PttavmProductRequest>();

        // Her varyant icin urun olustur
        foreach (var variant in product.ProductVariants)
        {
            var images = variant.Images?
                .OrderBy(img => img.DisplayOrder)
                .Select((img, idx) => new PttavmImageRequest(
                    fileStorage.GetPublicUrl(img.StorageKey ?? string.Empty),
                    idx + 1))
                .ToList() ?? new List<PttavmImageRequest>();

            var totalStock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
            var vatRate = (int)variant.VatRate;

            var request = new PttavmProductRequest(
                CategoryId: categoryMatch.MarketPlaceCategoryId,
                Barcode: variant.Barcode,
                Name: product.Title,
                PriceWithoutVat: variant.SalePrice,
                VatRate: vatRate,
                PriceWithVat: variant.SalePrice * (1 + (vatRate / 100m)),
                Quantity: totalStock,
                Desi: null,
                Variants: null,
                Images: images,
                NoShippingProduct: null,
                WarrantyDuration: null,
                BasketMaxQuantity: null);

            requests.Add(request);
        }

        if (requests.Count == 0)
            return new ErrorDataResult<List<PttavmProductRequest>>(null, "Ürünün varyantı yok.");

        logger.LogInformation("PttAVM product mapped: {ProductId}, {Count} variants", productId, requests.Count);
        return new SuccessDataResult<List<PttavmProductRequest>>(requests);
    }
}
