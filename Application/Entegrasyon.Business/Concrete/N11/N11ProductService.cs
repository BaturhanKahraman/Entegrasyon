using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 SOAP API ile ürün işlemleri gerçekleştiren legacy servis.
/// REST tercihi N11RestProductService (varsayılan). Bu servis yalnızca
/// `N11:UseSoap=true` flag'i ile aktif olur. Integration testlerinde HTTP
/// mock'lama WireMock.Net ile yapılır.
/// </summary>
public sealed class N11ProductService(
    IN11SoapClient soapClient,
    IN11ProductMapper productMapper,
    N11MappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<N11ProductService> logger) : IN11ProductService
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
            var errorMessage = response.Element("result")?.Element("errorMessage")?.Value
                ?? "Bilinmeyen N11 hatası";
            return new ErrorResult(errorMessage);
        }
        return new SuccessResult();
    }

    // -----------------------------------------------------------------------
    // SaveProductAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünü N11'e kaydeder (SaveProduct SOAP çağrısı).
    /// Önce mapping doğrulaması yapar, ardından XML oluşturup N11'e gönderir.
    /// Başarı durumunda N11 ürün ID'sini döner ve ProductMarketplace kaydını günceller.
    /// </summary>
    public async Task<IDataResult<long>> SaveProductAsync(Guid productId)
    {
        // Adım 1: Mapping doğrulaması
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        if (!validationResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                $"Eslestirme dogrulamasi basarisiz: {validationResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorDataResult<long>(0, validationResult.Message!);
        }

        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            "Eslestirme dogrulamasi basarili", ProductActivityStatus.Success, marketplaceName: "N11");

        // Adım 2: XML mapping
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
                $"Urun mapping hatasi: {mapResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorDataResult<long>(0, mapResult.Message!);
        }

        // Adım 3: SOAP isteği
        var request = new XElement(Ns + "SaveProductRequest", mapResult.Data);

        var response = await soapClient.SendAsync("ProductService", "", request);

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 SaveProduct başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                $"N11 kayıt hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorDataResult<long>(0, statusCheck.Message!);
        }

        // Adım 4: N11 ürün ID'sini parse et
        var n11ProductIdStr = response.Element("product")?.Element("id")?.Value;
        if (!long.TryParse(n11ProductIdStr, out var n11ProductId))
        {
            logger.LogError("N11 SaveProduct yanıtında ürün ID bulunamadı — ProductId={ProductId}", productId);
            return new ErrorDataResult<long>(0, "N11 yanıtında ürün ID bulunamadı.");
        }

        // Adım 5: ProductMarketplace güncelle veya oluştur
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm is null)
        {
            pm = new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = N11MarketPlaceId
            };
            dbContext.ProductMarketplaces.Add(pm);
        }

        pm.ExternalProductId = n11ProductId.ToString();
        pm.Status = MarketplaceProductStatus.Published;
        pm.LastSyncedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"Ürün N11'e kaydedildi (N11 ID: {n11ProductId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: n11ProductId.ToString());

        logger.LogInformation("N11 SaveProduct başarılı — ProductId={ProductId}, N11Id={N11Id}",
            productId, n11ProductId);

        return new SuccessDataResult<long>(n11ProductId, "Ürün N11'e kaydedildi.");
    }

    // -----------------------------------------------------------------------
    // DeleteProductAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünü N11'den siler (DeleteProductById SOAP çağrısı).
    /// ExternalProductId yoksa işlem yapılmaz.
    /// </summary>
    public async Task<IResult> DeleteProductAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
        {
            return new ErrorResult("N11 ExternalProductId bulunamadı — ürün N11'e gönderilmemiş olabilir.");
        }

        var externalId = pm.ExternalProductId;

        var request = new XElement(Ns + "DeleteProductByIdRequest",
            new XElement("productId", externalId));

        var response = await soapClient.SendAsync("ProductService", "", request);

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 DeleteProduct başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.Deleted,
                $"N11 silme hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(statusCheck.Message!);
        }

        pm.Status = MarketplaceProductStatus.Pending;
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.Deleted,
            $"Ürün N11'den silindi (N11 ID: {externalId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: externalId);

        logger.LogInformation("N11 DeleteProduct başarılı — ProductId={ProductId}, N11Id={N11Id}",
            productId, externalId);

        return new SuccessResult("Ürün N11'den silindi.");
    }

    // -----------------------------------------------------------------------
    // UpdateProductBasicAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün temel bilgilerini (fiyat, stok, açıklama) N11'de günceller.
    /// Tam mapper kullanmaz — sadece gerekli alanları inline oluşturur.
    /// </summary>
    public async Task<IResult> UpdateProductBasicAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
            return new ErrorResult("N11 ExternalProductId bulunamadı.");

        var externalId = pm.ExternalProductId;
        var firstVariant = product.ProductVariants.FirstOrDefault();
        if (firstVariant is null)
            return new ErrorResult("Ürünün varyantı yok.");

        var listPrice = firstVariant.ListPrice;
        var salePrice = firstVariant.SalePrice > listPrice ? listPrice : firstVariant.SalePrice;
        var totalStock = firstVariant.BranchOfficeStocks.Sum(s => s.CurrentStock);
        var barcode = firstVariant.Barcode ?? firstVariant.Id.ToString();

        var stockItemElements = product.ProductVariants.Select(variant =>
        {
            var variantSalePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;
            var variantStock = variant.BranchOfficeStocks.Sum(s => s.CurrentStock);
            var variantBarcode = variant.Barcode ?? variant.Id.ToString();

            return new XElement("stockItem",
                new XElement("sellerStockCode", variantBarcode),
                new XElement("quantity", variantStock),
                new XElement("optionPrice", variantSalePrice.ToString("F0")));
        }).ToList();

        var request = new XElement(Ns + "UpdateProductBasicRequest",
            new XElement("productId", externalId),
            new XElement("price", listPrice.ToString("F0")),
            new XElement("description", product.Description ?? string.Empty),
            new XElement("stockItems", stockItemElements));

        var response = await soapClient.SendAsync("ProductService", "", request);

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 UpdateProductBasic başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                $"N11 güncelleme hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(statusCheck.Message!);
        }

        pm.LastSyncedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Ürün temel bilgileri N11'de güncellendi (N11 ID: {externalId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: externalId);

        logger.LogInformation("N11 UpdateProductBasic başarılı — ProductId={ProductId}", productId);

        return new SuccessResult("Ürün N11'de güncellendi.");
    }

    // -----------------------------------------------------------------------
    // StartSellingAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün N11'deki satışını başlatır (StartSellingProductByProductId SOAP çağrısı).
    /// ProductSellingService WSDL kullanır.
    /// </summary>
    public async Task<IResult> StartSellingAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
            return new ErrorResult("N11 ExternalProductId bulunamadı.");

        var externalId = pm.ExternalProductId;

        var request = new XElement(Ns + "StartSellingProductByProductIdRequest",
            new XElement("productId", externalId));

        var response = await soapClient.SendAsync("ProductSellingService", "", request);

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 StartSelling başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                $"N11 satış başlatma hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(statusCheck.Message!);
        }

        pm.Status = MarketplaceProductStatus.Published;
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"N11'de satış başlatıldı (N11 ID: {externalId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: externalId);

        logger.LogInformation("N11 StartSelling başarılı — ProductId={ProductId}", productId);

        return new SuccessResult("N11'de satış başlatıldı.");
    }

    // -----------------------------------------------------------------------
    // StopSellingAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünün N11'deki satışını durdurur (StopSellingProductByProductId SOAP çağrısı).
    /// ProductSellingService WSDL kullanır.
    /// </summary>
    public async Task<IResult> StopSellingAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
            return new ErrorResult("N11 ExternalProductId bulunamadı.");

        var externalId = pm.ExternalProductId;

        var request = new XElement(Ns + "StopSellingProductByProductIdRequest",
            new XElement("productId", externalId));

        var response = await soapClient.SendAsync("ProductSellingService", "", request);

        var statusCheck = CheckN11ResponseStatus(response);
        if (!statusCheck.Success)
        {
            logger.LogError("N11 StopSelling başarısız — ProductId={ProductId}, Message={Message}",
                productId, statusCheck.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                $"N11 satış durdurma hatası: {statusCheck.Message}",
                ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(statusCheck.Message!);
        }

        pm.Status = MarketplaceProductStatus.Pending;
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"N11'de satış durduruldu (N11 ID: {externalId})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: externalId);

        logger.LogInformation("N11 StopSelling başarılı — ProductId={ProductId}", productId);

        return new SuccessResult("N11'de satış durduruldu.");
    }
}
