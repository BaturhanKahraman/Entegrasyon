using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada listing yönetimi servisi.
/// Fiyat/stok/kargo güncelleme, activate/deactivate.
/// Listing API ayrı base URL kullanır: listing-external.hepsiburada.com
/// Bu implementasyon catalog API client'ını kullanır — listing URL'leri full path ile gönderilir.
/// </summary>
public sealed class HepsiburadaListingService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHepsiburadaApiClient apiClient,
    ILogger<HepsiburadaListingService> logger) : IHepsiburadaListingService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    private async Task<string> GetMerchantIdAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == HbMarketPlaceId)
            ?? throw new InvalidOperationException("Hepsiburada marketplace kaydı bulunamadı.");
        return marketplace.SellerId ?? throw new InvalidOperationException("Hepsiburada merchantId (SellerId) tanımlı değil.");
    }

    public async Task<IDataResult<HepsiburadaListingResponse>> GetListingsAsync(int offset = 0, int limit = 100)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.GetAsync(
                $"/listings/merchantid/{merchantId}?offset={offset}&limit={limit}");

            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<HepsiburadaListingResponse>(null!, $"Listing API hatası: {response.StatusCode}");

            var data = await response.Content.ReadFromJsonAsync<HepsiburadaListingResponse>();
            return new SuccessDataResult<HepsiburadaListingResponse>(data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB listing query failed");
            return new ErrorDataResult<HepsiburadaListingResponse>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UpdatePricesAsync(List<HepsiburadaPriceUpdateItem> items)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync(
                $"/listings/merchantid/{merchantId}/price-uploads", items);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("HB price update failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorResult($"Fiyat güncelleme hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<HepsiburadaListingUpdateResponse>();
            if (result?.Errors?.Any() == true)
            {
                var errorMessages = result.Errors.SelectMany(e => e.Errors ?? []).ToList();
                logger.LogWarning("HB price update partial errors: {Errors}", string.Join(", ", errorMessages));
                return new ErrorResult($"Bazı fiyatlar güncellenemedi: {string.Join(", ", errorMessages)}");
            }

            logger.LogInformation("HB prices updated: {Count} items", items.Count);
            return new SuccessResult($"{items.Count} fiyat güncellendi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB price update exception");
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UpdateStocksAsync(List<HepsiburadaStockUpdateItem> items)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync(
                $"/listings/merchantid/{merchantId}/stock-uploads", items);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("HB stock update failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorResult($"Stok güncelleme hatası: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<HepsiburadaListingUpdateResponse>();
            if (result?.Errors?.Any() == true)
            {
                var errorMessages = result.Errors.SelectMany(e => e.Errors ?? []).ToList();
                return new ErrorResult($"Bazı stoklar güncellenemedi: {string.Join(", ", errorMessages)}");
            }

            logger.LogInformation("HB stocks updated: {Count} items", items.Count);
            return new SuccessResult($"{items.Count} stok güncellendi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB stock update exception");
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> UpdateShippingInfoAsync(List<HepsiburadaShippingInfoUpdateItem> items)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync(
                $"/listings/merchantid/{merchantId}/shipping-info-uploads", items);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("HB shipping update failed: {Status} {Error}", response.StatusCode, error);
                return new ErrorResult($"Teslimat bilgisi güncelleme hatası: {response.StatusCode}");
            }

            logger.LogInformation("HB shipping info updated: {Count} items", items.Count);
            return new SuccessResult($"{items.Count} teslimat bilgisi güncellendi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB shipping info update exception");
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> ActivateListingAsync(string hepsiburadaSku, string merchantSku)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync(
                $"/listings/merchantid/{merchantId}/sku/{hepsiburadaSku}/activate",
                new { merchantSku });

            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Listing activate hatası: {response.StatusCode}");

            logger.LogInformation("HB listing activated: {Sku}", hepsiburadaSku);
            return new SuccessResult("Listing satışa açıldı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB listing activate exception");
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> DeactivateListingAsync(string hepsiburadaSku, string merchantSku)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var response = await apiClient.PostAsync(
                $"/listings/merchantid/{merchantId}/sku/{hepsiburadaSku}/deactivate",
                new { merchantSku });

            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Listing deactivate hatası: {response.StatusCode}");

            logger.LogInformation("HB listing deactivated: {Sku}", hepsiburadaSku);
            return new SuccessResult("Listing satıştan kapatıldı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB listing deactivate exception");
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> SyncProductStockAndPriceAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var productMarketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId &&
                                       pm.MarketPlaceId == HbMarketPlaceId &&
                                       pm.Status == MarketplaceProductStatus.Published);

        if (productMarketplace?.ExternalProductId == null)
            return new ErrorResult("Ürün Hepsiburada'da yayında değil veya HB SKU bulunamadı.");

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return new ErrorResult("Ürün bulunamadı.");

        // Warehouse config
        var warehouseBranchIds = await dbContext.MarketPlaceWarehouses
            .Where(w => w.MarketPlaceId == HbMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        if (!warehouseBranchIds.Any())
        {
            var defaultBranch = await dbContext.BranchOffices
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();
            if (defaultBranch > 0) warehouseBranchIds.Add(defaultBranch);
        }

        var priceItems = new List<HepsiburadaPriceUpdateItem>();
        var stockItems = new List<HepsiburadaStockUpdateItem>();

        foreach (var variant in product.ProductVariants)
        {
            var hbSku = productMarketplace.ExternalProductId;
            var merchantSku = (variant.Barcode ?? variant.Id.ToString()).ToUpperInvariant();

            var stock = variant.BranchOfficeStocks
                .Where(s => warehouseBranchIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);

            priceItems.Add(new HepsiburadaPriceUpdateItem(hbSku, merchantSku, variant.SalePrice));
            stockItems.Add(new HepsiburadaStockUpdateItem(hbSku, merchantSku, stock, null));
        }

        var priceResult = await UpdatePricesAsync(priceItems);
        var stockResult = await UpdateStocksAsync(stockItems);

        if (!priceResult.Success && !stockResult.Success)
            return new ErrorResult($"Fiyat: {priceResult.Message} | Stok: {stockResult.Message}");

        if (!priceResult.Success)
            return new ErrorResult($"Fiyat güncelleme başarısız: {priceResult.Message}");

        if (!stockResult.Success)
            return new ErrorResult($"Stok güncelleme başarısız: {stockResult.Message}");

        return new SuccessResult("Fiyat ve stok senkronize edildi.");
    }
}
