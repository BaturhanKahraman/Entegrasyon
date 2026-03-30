using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 REST API ile fiyat ve stok güncelleme işlemlerini gerçekleştiren servis.
/// price-stock-update task-based endpoint'ini kullanır.
/// </summary>
public sealed class N11RestStockPriceService(
    IN11RestClient restClient,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IProductActivityLogger activityLogger,
    ILogger<N11RestStockPriceService> logger) : IN11StockPriceService
{
    private const string Integrator = "Entegrasyon";

    // -----------------------------------------------------------------------
    // UpdatePriceAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün N11'deki fiyatını REST task olarak günceller.
    /// Tüm varyantların stockCode'larını DB'den çeker.
    /// </summary>
    public async Task<IResult> UpdatePriceAsync(Guid productId, decimal newPrice)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm is null)
            return new ErrorResult("N11 ProductMarketplace kaydı bulunamadı.");

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var listPrice = product.ProductVariants.FirstOrDefault()?.ListPrice ?? newPrice;
        // listPrice >= salePrice kuralı
        var salePrice = newPrice > listPrice ? listPrice : newPrice;

        var skus = product.ProductVariants.Select(v => new N11PriceStockSku(
            StockCode: v.Barcode ?? v.Id.ToString(),
            ListPrice: Math.Round(listPrice, 2),
            SalePrice: Math.Round(salePrice, 2),
            CurrencyType: "TL")).ToList();

        var request = new N11PriceStockUpdateRequest(new N11PriceStockPayload(Integrator, skus));
        var taskResponse = await restClient.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
            "ms/product/tasks/price-stock-update", request);

        if (taskResponse is null)
        {
            logger.LogError("N11 REST UpdatePrice yanıt boş — ProductId={ProductId}", productId);
            await activityLogger.LogAsync(productId, ProductActivityType.PriceUpdated,
                "N11 REST fiyat güncelleme yanıt boş döndü", ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult("N11 REST API yanıt vermedi.");
        }

        pm.BatchRequestId = taskResponse.Id.ToString();
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PriceUpdated,
            $"N11 REST fiyat güncelleme task gönderildi: {salePrice:F2} TL (TaskId: {taskResponse.Id})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: taskResponse.Id.ToString());

        logger.LogInformation("N11 REST UpdatePrice başarılı — ProductId={ProductId}, Price={Price}, TaskId={TaskId}",
            productId, salePrice, taskResponse.Id);

        return new SuccessResult("Fiyat N11'e gönderildi. Task işleniyor...");
    }

    // -----------------------------------------------------------------------
    // UpdateStockAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün N11'deki stok miktarını REST task olarak günceller.
    /// Tüm varyantların stockCode'larını DB'den çeker.
    /// </summary>
    public async Task<IResult> UpdateStockAsync(Guid productId, int quantity)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm is null)
            return new ErrorResult("N11 ProductMarketplace kaydı bulunamadı.");

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var skus = product.ProductVariants.Select(v => new N11PriceStockSku(
            StockCode: v.Barcode ?? v.Id.ToString(),
            Quantity: quantity)).ToList();

        var request = new N11PriceStockUpdateRequest(new N11PriceStockPayload(Integrator, skus));
        var taskResponse = await restClient.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
            "ms/product/tasks/price-stock-update", request);

        if (taskResponse is null)
        {
            logger.LogError("N11 REST UpdateStock yanıt boş — ProductId={ProductId}", productId);
            await activityLogger.LogAsync(productId, ProductActivityType.StockUpdated,
                "N11 REST stok güncelleme yanıt boş döndü", ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult("N11 REST API yanıt vermedi.");
        }

        pm.BatchRequestId = taskResponse.Id.ToString();
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.StockUpdated,
            $"N11 REST stok güncelleme task gönderildi: {quantity} adet (TaskId: {taskResponse.Id})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: taskResponse.Id.ToString());

        logger.LogInformation("N11 REST UpdateStock başarılı — ProductId={ProductId}, Quantity={Quantity}, TaskId={TaskId}",
            productId, quantity, taskResponse.Id);

        return new SuccessResult("Stok N11'e gönderildi. Task işleniyor...");
    }
}
