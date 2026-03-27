using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 SOAP API ile gerçek fiyat ve stok güncelleme işlemlerini gerçekleştiren servis.
/// MockN11StockPriceService'in production uygulamasıdır.
/// </summary>
public sealed class N11StockPriceService(
    IN11SoapClient soapClient,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IProductActivityLogger activityLogger,
    ILogger<N11StockPriceService> logger) : IN11StockPriceService
{
    private static readonly XNamespace Ns = "http://www.n11.com/ws/schemas";

    // -----------------------------------------------------------------------
    // Yardımcı metod
    // -----------------------------------------------------------------------

    /// <summary>
    /// N11 SOAP yanıtındaki result/status alanını kontrol eder.
    /// Failure ise ErrorResult, başarı ise SuccessResult döner.
    /// </summary>
    private static IResult CheckN11ResponseStatus(XElement response)
    {
        var status = response.Element("result")?.Element("status")?.Value;
        if (status == "failure")
        {
            var errorMessage = response.Element("result")?.Element("errorMessage")?.Value ?? "N11 hatası";
            return new ErrorResult(errorMessage);
        }
        return new SuccessResult();
    }

    // -----------------------------------------------------------------------
    // UpdatePriceAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün N11'deki fiyatını günceller (UpdateProductPriceById SOAP çağrısı).
    /// ProductService WSDL kullanır. ExternalProductId yoksa işlem yapılmaz.
    /// </summary>
    public async Task<IResult> UpdatePriceAsync(Guid productId, decimal newPrice)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Adım 1: N11 ürün ID'sini bul
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
        {
            return new ErrorResult("N11 ExternalProductId bulunamadı — ürün N11'e gönderilmemiş olabilir.");
        }

        var externalId = pm.ExternalProductId;

        // Adım 2: Ürün varyantlarını stok kalemleri için çek
        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var stockItemElements = product.ProductVariants.Select(variant =>
        {
            var barcode = variant.Barcode ?? variant.Id.ToString();
            return new XElement("stockItem",
                new XElement("sellerStockCode", barcode),
                new XElement("optionPrice", newPrice.ToString("F2")));
        }).ToList();

        // Adım 3: SOAP isteği oluştur
        var request = new XElement(Ns + "UpdateProductPriceByIdRequest",
            new XElement("productId", externalId),
            new XElement("price", newPrice.ToString("F2")),
            new XElement("currencyType", "1"),
            new XElement("stockItems", stockItemElements));

        // Adım 4: Gönder
        var response = await soapClient.SendAsync("ProductService", "", request);

        // Adım 5: Yanıt kontrolü
        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 UpdatePrice başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.PriceUpdated,
                $"N11 fiyat güncelleme hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(statusCheck.Message!);
        }

        // Adım 6: Aktivite logu
        await activityLogger.LogAsync(productId, ProductActivityType.PriceUpdated,
            $"Fiyat N11'de güncellendi: {newPrice:F2} TL (N11 ID: {externalId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: externalId);

        logger.LogInformation("N11 UpdatePrice başarılı — ProductId={ProductId}, Price={Price}",
            productId, newPrice);

        return new SuccessResult("Fiyat N11'de güncellendi.");
    }

    // -----------------------------------------------------------------------
    // UpdateStockAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün N11'deki stok miktarını günceller (UpdateStockBySellerStockCode SOAP çağrısı).
    /// ProductStockService WSDL kullanır. ExternalProductId yoksa işlem yapılmaz.
    /// Optimistic concurrency için önce GetProductStockByProductId ile mevcut version bilgisi çekilir.
    /// Ürün N11'de bulunamazsa version=0 ile devam edilir.
    /// </summary>
    public async Task<IResult> UpdateStockAsync(Guid productId, int quantity)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Adım 1: N11 ürün ID'sini bul
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
        {
            return new ErrorResult("N11 ExternalProductId bulunamadı — ürün N11'e gönderilmemiş olabilir.");
        }

        var externalId = pm.ExternalProductId;

        // Adım 2: Ürün varyantlarını sellerStockCode için çek
        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        // Adım 3: Mevcut stok versiyonlarını N11'den çek (optimistic concurrency)
        var stockVersions = await FetchStockVersionsAsync(externalId);

        // Adım 4: SOAP isteğini version bilgileriyle oluştur
        var stockItemElements = product.ProductVariants.Select(variant =>
        {
            var barcode = variant.Barcode ?? variant.Id.ToString();
            stockVersions.TryGetValue(barcode, out var version);
            return new XElement("stockItem",
                new XElement("sellerStockCode", barcode),
                new XElement("quantity", quantity),
                new XElement("version", version.ToString()));
        }).ToList();

        // Adım 5: SOAP isteği oluştur
        var request = new XElement(Ns + "UpdateStockBySellerStockCodeRequest",
            new XElement("stockItems", stockItemElements));

        // Adım 6: Gönder
        var response = await soapClient.SendAsync("ProductStockService", "", request);

        // Adım 7: Yanıt kontrolü
        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 UpdateStock başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.StockUpdated,
                $"N11 stok güncelleme hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(statusCheck.Message!);
        }

        // Adım 8: Aktivite logu
        await activityLogger.LogAsync(productId, ProductActivityType.StockUpdated,
            $"Stok N11'de güncellendi: {quantity} adet (N11 ID: {externalId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: externalId);

        logger.LogInformation("N11 UpdateStock başarılı — ProductId={ProductId}, Quantity={Quantity}",
            productId, quantity);

        return new SuccessResult("Stok N11'de güncellendi.");
    }

    // -----------------------------------------------------------------------
    // FetchStockVersionsAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// N11'den ürünün mevcut stok kalemlerini ve version numaralarını çeker.
    /// sellerStockCode → version eşleşmesini dictionary olarak döner.
    /// SOAP çağrısı başarısız olursa boş dictionary döner (version=0 fallback).
    /// </summary>
    private async Task<Dictionary<string, long>> FetchStockVersionsAsync(string externalProductId)
    {
        try
        {
            var getStockRequest = new XElement(Ns + "GetProductStockByProductIdRequest",
                new XElement("productId", externalProductId));

            var getStockResponse = await soapClient.SendAsync("ProductStockService", "", getStockRequest);

            var statusCheck = CheckN11ResponseStatus(getStockResponse);
            if (!statusCheck.Success)
            {
                logger.LogWarning(
                    "N11 GetProductStockByProductId başarısız — ExternalProductId={ExternalProductId}, fallback version=0 kullanılacak. Message={Message}",
                    externalProductId, statusCheck.Message);
                return [];
            }

            var versions = new Dictionary<string, long>();
            var stockItems = getStockResponse.Descendants("stockItem");

            foreach (var item in stockItems)
            {
                var sellerStockCode = item.Element("sellerStockCode")?.Value;
                var versionStr = item.Element("version")?.Value;

                if (sellerStockCode is not null && long.TryParse(versionStr, out var version))
                {
                    versions[sellerStockCode] = version;
                }
            }

            return versions;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "N11 GetProductStockByProductId çağrısı exception ile sonuçlandı — ExternalProductId={ExternalProductId}, fallback version=0 kullanılacak.",
                externalProductId);
            return [];
        }
    }
}
