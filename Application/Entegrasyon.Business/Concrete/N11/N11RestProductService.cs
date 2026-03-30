using System.Diagnostics;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Diagnostics;
using Entegrasyon.Business.FileStorage;
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
/// N11 REST API ile ürün işlemlerini gerçekleştiren servis.
/// SaveProduct, UpdateProduct, StartSelling, StopSelling → task-based REST endpoint'ler.
/// DeleteProduct → SOAP API (REST karşılığı yok).
/// </summary>
public sealed class N11RestProductService(
    IN11RestClient restClient,
    IN11SoapClient soapClient,
    N11MappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMinioFileStorage fileStorage,
    ILogger<N11RestProductService> logger) : IN11ProductService
{
    private const string Integrator = "Entegrasyon";

    // -----------------------------------------------------------------------
    // SaveProductAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ürünü N11 REST API'ye task olarak gönderir.
    /// Başarıda taskId, BatchRequestId'ye kaydedilir; Status=Pending olarak bırakılır.
    /// N11TaskPollingService arka planda task sonucunu takip eder.
    /// </summary>
    public async Task<IDataResult<long>> SaveProductAsync(Guid productId)
    {
        var sw = Stopwatch.StartNew();
        using var activity = EntegrasyonActivitySource.StartProductSync("N11", productId);
        try
        {
        // Adım 1: Mapping doğrulaması
        using (var validationSpan = EntegrasyonActivitySource.StartValidation("N11"))
        {
            var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
            validationSpan?.SetTag("validation.success", validationResult.Success);

            if (!validationResult.Success)
            {
                validationSpan?.SetStatus(ActivityStatusCode.Error, validationResult.Message);
                activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                    $"Eşleştirme doğrulaması başarısız: {validationResult.Message}",
                    ProductActivityStatus.Error, marketplaceName: "N11");
                EntegrasyonMetrics.ProductSyncErrors.Add(1,
                    new KeyValuePair<string, object?>("marketplace", "N11"),
                    new KeyValuePair<string, object?>("error_type", "validation"));
                return new ErrorDataResult<long>(0, validationResult.Message!);
            }
        }

        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            "Eşleştirme doğrulaması başarılı", ProductActivityStatus.Success, marketplaceName: "N11");

        // Adım 2: Ürün verisini yükle
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants).ThenInclude(v => v.BranchOfficeStocks).ThenInclude(s => s.BranchOffice)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .Include(p => p.AttributeKeyValues).ThenInclude(a => a.CategoryAttribute)
            .Include(p => p.AttributeKeyValues).ThenInclude(a => a.AttributeValue)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<long>(0, "Ürün bulunamadı.");

        var pm = await dbContext.ProductMarketplaces
            .Include(x => x.VariantOverrides)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        // Adım 3: Kategori eşleştirmesi
        var categoryMatch = await dbContext.CategoryMarketPlaceMatches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ApplicationCategoryId == product.CategoryId && m.MarketPlaceId == N11MarketPlaceId);

        if (categoryMatch is null)
            return new ErrorDataResult<long>(0, "Kategori N11 eşleştirmesi bulunamadı.");

        // Adım 4: Özellik eşleştirmeleri
        var attributeIds = product.AttributeKeyValues.Select(a => a.CategoryAttributeId).Distinct().ToList();
        var attributeMatches = await dbContext.CategoryAttributeMarketPlaceMatches
            .AsNoTracking()
            .Where(m => attributeIds.Contains(m.ApplicationCategoryAttributeId) && m.MarketPlaceId == N11MarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeId, m => m.MarketPlaceCategoryAttributeId);

        var valueIds = product.AttributeKeyValues
            .Where(a => a.AttributeValueId.HasValue)
            .Select(a => a.AttributeValueId!.Value)
            .Distinct().ToList();
        var valueMatches = await dbContext.CategoryAttributeValueMarketPlaceMatches
            .AsNoTracking()
            .Where(m => valueIds.Contains(m.ApplicationCategoryAttributeValueId) && m.MarketPlaceId == N11MarketPlaceId)
            .ToDictionaryAsync(m => m.ApplicationCategoryAttributeValueId, m => m.MarketPlaceCategoryAttributeValueId);

        // Adım 5: Depo listesi
        var warehouseIds = await dbContext.MarketPlaceWarehouses
            .AsNoTracking()
            .Where(w => w.MarketPlaceId == N11MarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        if (warehouseIds.Count == 0)
        {
            warehouseIds = await dbContext.BranchOffices
                .AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync();
        }

        // Adım 6: SKU listesini oluştur
        var effectiveTitle = pm?.TitleOverride ?? product.Title;
        var effectiveDescription = pm?.DescriptionOverride ?? product.Description ?? string.Empty;

        var skus = BuildSkus(product, pm, categoryMatch, attributeMatches, valueMatches, warehouseIds,
            effectiveTitle, effectiveDescription);

        if (skus.Count == 0)
            return new ErrorDataResult<long>(0, "Hiçbir varyant N11'e gönderilemedi.");

        // Adım 7: REST isteği gönder
        var request = new N11CreateProductRequest(new N11ProductPayload(Integrator, skus));
        var apiSw = Stopwatch.StartNew();
        var taskResponse = await restClient.PostAsync<N11CreateProductRequest, N11TaskResponse>(
            "ms/product/tasks/product-create", request);
        apiSw.Stop();
        EntegrasyonMetrics.MarketplaceApiDuration.Record(apiSw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("marketplace", "N11"),
            new KeyValuePair<string, object?>("operation", "publish"));

        if (taskResponse is null)
        {
            logger.LogError("N11 REST SaveProduct yanıt boş döndü — ProductId={ProductId}", productId);
            activity?.SetStatus(ActivityStatusCode.Error, "N11 REST API yanıt vermedi");
            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                "N11 REST yanıt boş döndü", ProductActivityStatus.Error, marketplaceName: "N11");
            EntegrasyonMetrics.ProductSyncErrors.Add(1,
                new KeyValuePair<string, object?>("marketplace", "N11"),
                new KeyValuePair<string, object?>("error_type", "no_response"));
            return new ErrorDataResult<long>(0, "N11 REST API yanıt vermedi.");
        }

        // Adım 8: ProductMarketplace kaydını güncelle
        if (pm is null)
        {
            pm = new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = N11MarketPlaceId
            };
            dbContext.ProductMarketplaces.Add(pm);
        }

        pm.BatchRequestId = taskResponse.Id.ToString();
        pm.Status = MarketplaceProductStatus.Pending;
        pm.LastSyncedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"N11 REST ürün gönderildi (TaskId: {taskResponse.Id})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: taskResponse.Id.ToString());

        logger.LogInformation("N11 REST SaveProduct başarılı — ProductId={ProductId}, TaskId={TaskId}",
            productId, taskResponse.Id);

        activity?.SetTag("n11.task_id", taskResponse.Id);
        sw.Stop();
        EntegrasyonMetrics.ProductSyncTotal.Add(1,
            new KeyValuePair<string, object?>("marketplace", "N11"));
        EntegrasyonMetrics.ProductSyncDuration.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("marketplace", "N11"));

        activity?.SetStatus(ActivityStatusCode.Ok);
        return new SuccessDataResult<long>(taskResponse.Id, "Ürün N11'e gönderildi. Task işleniyor...");
        } // try
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            EntegrasyonMetrics.ProductSyncErrors.Add(1,
                new KeyValuePair<string, object?>("marketplace", "N11"),
                new KeyValuePair<string, object?>("error_type", "exception"));
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // DeleteProductAsync — SOAP fallback
    // -----------------------------------------------------------------------

    public async Task<IResult> DeleteProductAsync(Guid productId)
    {
        // N11 REST API'de ürün silme endpoint'i yok — SOAP kullanılır
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm?.ExternalProductId is null)
            return new ErrorResult("N11 ExternalProductId bulunamadı — ürün N11'e gönderilmemiş olabilir.");

        var externalId = pm.ExternalProductId;
        var ns = System.Xml.Linq.XNamespace.Get("http://www.n11.com/ws/schemas");

        var request = new System.Xml.Linq.XElement(ns + "DeleteProductByIdRequest",
            new System.Xml.Linq.XElement("productId", externalId));

        System.Xml.Linq.XElement response;
        try
        {
            response = await soapClient.SendAsync("ProductService", "", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "N11 SOAP DeleteProduct başarısız — ProductId={ProductId}", productId);
            return new ErrorResult($"N11 SOAP bağlantı hatası: {ex.Message}");
        }

        var status = response.Element("result")?.Element("status")?.Value;
        if (status == "failure")
        {
            var msg = response.Element("result")?.Element("errorMessage")?.Value ?? "N11 SOAP silme hatası";
            logger.LogError("N11 DeleteProduct başarısız — ProductId={ProductId}, Message={Message}", productId, msg);
            await activityLogger.LogAsync(productId, ProductActivityType.Deleted,
                $"N11 silme hatası: {msg}", ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult(msg);
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

    public async Task<IResult> UpdateProductBasicAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm is null)
            return new ErrorResult("N11 ProductMarketplace kaydı bulunamadı.");

        var skus = product.ProductVariants.Select(v => new N11UpdateProductSku(
            StockCode: v.Barcode ?? v.Id.ToString(),
            Description: product.Description)).ToList();

        var request = new N11UpdateProductRequest(new N11UpdateProductPayload(Integrator, skus));
        var taskResponse = await restClient.PostAsync<N11UpdateProductRequest, N11TaskResponse>(
            "ms/product/tasks/product-update", request);

        if (taskResponse is null)
        {
            logger.LogError("N11 REST UpdateProductBasic yanıt boş — ProductId={ProductId}", productId);
            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                "N11 REST güncelleme yanıt boş döndü", ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult("N11 REST API yanıt vermedi.");
        }

        pm.BatchRequestId = taskResponse.Id.ToString();
        pm.Status = MarketplaceProductStatus.Pending;
        pm.LastSyncedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"N11 REST ürün güncelleme task gönderildi (TaskId: {taskResponse.Id})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: taskResponse.Id.ToString());

        logger.LogInformation("N11 REST UpdateProductBasic başarılı — ProductId={ProductId}, TaskId={TaskId}",
            productId, taskResponse.Id);

        return new SuccessResult("Ürün güncelleme N11'e gönderildi. Task işleniyor...");
    }

    // -----------------------------------------------------------------------
    // StartSellingAsync
    // -----------------------------------------------------------------------

    public async Task<IResult> StartSellingAsync(Guid productId)
        => await SetSellingStatusAsync(productId, "Active");

    // -----------------------------------------------------------------------
    // StopSellingAsync
    // -----------------------------------------------------------------------

    public async Task<IResult> StopSellingAsync(Guid productId)
        => await SetSellingStatusAsync(productId, "Suspended");

    // -----------------------------------------------------------------------
    // Yardımcı: SetSellingStatusAsync
    // -----------------------------------------------------------------------

    private async Task<IResult> SetSellingStatusAsync(Guid productId, string status)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == N11MarketPlaceId);

        if (pm is null)
            return new ErrorResult("N11 ProductMarketplace kaydı bulunamadı.");

        var skus = product.ProductVariants.Select(v => new N11UpdateProductSku(
            StockCode: v.Barcode ?? v.Id.ToString(),
            Status: status)).ToList();

        var request = new N11UpdateProductRequest(new N11UpdateProductPayload(Integrator, skus));
        var taskResponse = await restClient.PostAsync<N11UpdateProductRequest, N11TaskResponse>(
            "ms/product/tasks/product-update", request);

        if (taskResponse is null)
        {
            logger.LogError("N11 REST SetSellingStatus yanıt boş — ProductId={ProductId}, Status={Status}", productId, status);
            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                $"N11 REST {status} yanıt boş döndü", ProductActivityStatus.Error, marketplaceName: "N11");
            return new ErrorResult("N11 REST API yanıt vermedi.");
        }

        pm.BatchRequestId = taskResponse.Id.ToString();
        pm.Status = MarketplaceProductStatus.Pending;
        await dbContext.SaveChangesAsync();

        var action = status == "Active" ? "satış başlatıldı" : "satış durduruldu";
        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"N11 REST {action} task gönderildi (TaskId: {taskResponse.Id})",
            ProductActivityStatus.Success, marketplaceName: "N11", referenceId: taskResponse.Id.ToString());

        logger.LogInformation("N11 REST SetSellingStatus başarılı — ProductId={ProductId}, Status={Status}, TaskId={TaskId}",
            productId, status, taskResponse.Id);

        return new SuccessResult($"N11'de {action}. Task işleniyor...");
    }

    // -----------------------------------------------------------------------
    // SKU builder
    // -----------------------------------------------------------------------

    private List<N11ProductSku> BuildSkus(
        Entegrasyon.Entity.Products.Product product,
        ProductMarketplace? pm,
        Entegrasyon.Entity.Matches.CategoryMarketPlaceMatch categoryMatch,
        Dictionary<int, int> attributeMatches,
        Dictionary<int, int> valueMatches,
        List<int> warehouseIds,
        string title,
        string description)
    {
        var skus = new List<N11ProductSku>();

        foreach (var variant in product.ProductVariants)
        {
            var quantity = variant.BranchOfficeStocks
                .Where(s => warehouseIds.Contains(s.BranchOfficeId))
                .Sum(s => s.CurrentStock);

            var variantOverride = pm?.VariantOverrides?.FirstOrDefault(vo => vo.ProductVariantId == variant.Id);
            var effectiveListPrice = variantOverride?.ListPriceOverride ?? variant.ListPrice;
            var effectiveSalePrice = variantOverride?.SalePriceOverride ?? variant.SalePrice;
            var salePrice = effectiveSalePrice > effectiveListPrice ? effectiveListPrice : effectiveSalePrice;

            var images = variant.Images
                .Where(i => !string.IsNullOrEmpty(i.StorageKey))
                .OrderBy(i => i.DisplayOrder)
                .Select((img, idx) => new N11ImageDto(fileStorage.GetPublicUrl(img.StorageKey!), idx))
                .ToList();

            var attributes = product.AttributeKeyValues
                .Where(a => attributeMatches.ContainsKey(a.CategoryAttributeId))
                .Select(a =>
                {
                    long? valueId = null;
                    if (a.AttributeValueId.HasValue && valueMatches.TryGetValue(a.AttributeValueId.Value, out var mappedId))
                        valueId = mappedId;

                    string? customValue = !valueId.HasValue
                        ? (a.AttributeValue?.Name ?? a.CustomValue)
                        : null;

                    return new N11AttributeDto(attributeMatches[a.CategoryAttributeId], valueId, customValue);
                })
                .ToList();

            var barcode = variant.Barcode ?? variant.Id.ToString();
            var stockCode = variant.Barcode ?? variant.Id.ToString();

            skus.Add(new N11ProductSku(
                Title: title,
                Description: description,
                CategoryId: categoryMatch.MarketPlaceCategoryId,
                CurrencyType: "TL",
                ProductMainId: product.StockCode ?? product.Id.ToString(),
                PreparingDay: 3,
                ShipmentTemplate: "STANDART",
                StockCode: stockCode,
                CatalogId: null,
                Barcode: barcode,
                Quantity: quantity,
                Images: images,
                Attributes: attributes,
                SalePrice: salePrice,
                ListPrice: effectiveListPrice,
                VatRate: 10));
        }

        return skus;
    }
}
