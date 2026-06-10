using System.Diagnostics;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Diagnostics;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolProductService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolApiClient apiClient,
    ITrendyolProductMapper productMapper,
    TrendyolMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<TrendyolProductService> logger) : ITrendyolProductService
{
    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        var sw = Stopwatch.StartNew();
        using var activity = EntegrasyonActivitySource.StartProductSync("Trendyol", productId);
        try
        {
            // Adım 1: Mapping doğrulama
            bool validationSuccess;
            using (var validationSpan = EntegrasyonActivitySource.StartValidation("Trendyol"))
            {
                var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
                validationSuccess = validationResult.Success;
                validationSpan?.SetTag("validation.success", validationResult.Success);

                if (!validationResult.Success)
                {
                    validationSpan?.SetStatus(ActivityStatusCode.Error, validationResult.Message);
                    activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                    await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                        $"Eslestirme dogrulamasi başarısız: {validationResult.Message}",
                        ProductActivityStatus.Error, marketplaceName: "Trendyol");
                    EntegrasyonMetrics.ProductSyncErrors.Add(1,
                        new KeyValuePair<string, object?>("marketplace", "Trendyol"),
                        new KeyValuePair<string, object?>("error_type", "validation"));
                    return new ErrorDataResult<string>(null!, validationResult.Message!);
                }
            }

            await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                "Eslestirme dogrulamasi basarili", ProductActivityStatus.Success, marketplaceName: "Trendyol");

            // Adım 2: Mapping
            TrendyolCreateProductRequest mappedData;
            using (var mappingSpan = EntegrasyonActivitySource.StartMapping("Trendyol"))
            {
                var mapResult = await productMapper.MapProductAsync(productId);
                mappingSpan?.SetTag("mapping.success", mapResult.Success);

                if (!mapResult.Success)
                {
                    mappingSpan?.SetStatus(ActivityStatusCode.Error, mapResult.Message);
                    activity?.SetStatus(ActivityStatusCode.Error, "Mapping failed");
                    await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
                        $"Urun mapping hatasi: {mapResult.Message}",
                        ProductActivityStatus.Error, marketplaceName: "Trendyol");
                    EntegrasyonMetrics.ProductSyncErrors.Add(1,
                        new KeyValuePair<string, object?>("marketplace", "Trendyol"),
                        new KeyValuePair<string, object?>("error_type", "mapping"));
                    return new ErrorDataResult<string>(null!, mapResult.Message!);
                }

                mappedData = mapResult.Data;
            }

            await using var dbContext = await contextFactory.CreateDbContextAsync();

            var marketplace = await dbContext.MarketPlaces.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

            if (marketplace?.SellerId is null)
                return new ErrorDataResult<string>(null!, "Trendyol SellerId ayarlanmamis.");

            // Adım 3: HTTP çağrısı (HttpClient auto-instrumentation devreye girer)
            var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products";
            var apiSw = Stopwatch.StartNew();
            var response = await apiClient.PostAsync(url, mappedData);
            apiSw.Stop();
            EntegrasyonMetrics.MarketplaceApiDuration.Record(apiSw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("marketplace", "Trendyol"),
                new KeyValuePair<string, object?>("operation", "publish"));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Trendyol product publish failed. Status={Status}, Body={Body}",
                    response.StatusCode, errorBody);
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");

                await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                    $"Trendyol API hatasi: {response.StatusCode}",
                    ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");
                EntegrasyonMetrics.ProductSyncErrors.Add(1,
                    new KeyValuePair<string, object?>("marketplace", "Trendyol"),
                    new KeyValuePair<string, object?>("error_type", "api_error"));

                return new ErrorDataResult<string>(null!, $"Trendyol API hatasi: {response.StatusCode} -- {errorBody}");
            }

            var batchResponse = await response.Content.ReadFromJsonAsync<TrendyolBatchResponse>();
            var batchRequestId = batchResponse?.BatchRequestId
                ?? throw new InvalidOperationException("Trendyol batch response'da BatchRequestId bulunamadı.");

            activity?.SetTag("trendyol.batch_id", batchRequestId);
            activity?.SetTag("trendyol.variant_count", mappedData.Items.Count);

            var pm = await dbContext.ProductMarketplaces
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);

            if (pm is not null)
            {
                pm.BatchRequestId = batchRequestId;
                pm.StatusMessage = null;
                await dbContext.SaveChangesAsync();
            }

            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                $"Trendyol'a gonderildi -- {mappedData.Items.Count} varyant",
                ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: batchRequestId);

            sw.Stop();
            EntegrasyonMetrics.ProductSyncTotal.Add(1,
                new KeyValuePair<string, object?>("marketplace", "Trendyol"));
            EntegrasyonMetrics.ProductSyncDuration.Record(sw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("marketplace", "Trendyol"));

            activity?.SetStatus(ActivityStatusCode.Ok);
            return new SuccessDataResult<string>(batchRequestId, "Urun Trendyol'a gonderildi.");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            EntegrasyonMetrics.ProductSyncErrors.Add(1,
                new KeyValuePair<string, object?>("marketplace", "Trendyol"),
                new KeyValuePair<string, object?>("error_type", "exception"));
            throw;
        }
    }

    public async Task<IDataResult<TrendyolBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorDataResult<TrendyolBatchStatusResponse>(null!, "Trendyol SellerId ayarlanmamis.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/products/batch-requests/{batchRequestId}";
        var response = await apiClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol batch status check failed. BatchId={BatchId}, Status={Status}",
                batchRequestId, response.StatusCode);
            return new ErrorDataResult<TrendyolBatchStatusResponse>(null!, $"Trendyol API hatasi: {response.StatusCode}");
        }

        var statusResponse = await response.Content.ReadFromJsonAsync<TrendyolBatchStatusResponse>();
        if (statusResponse is null)
            return new ErrorDataResult<TrendyolBatchStatusResponse>(null!, "Trendyol batch status response parse edilemedi.");

        return new SuccessDataResult<TrendyolBatchStatusResponse>(statusResponse);
    }

    public async Task<IResult> UpdateUnapprovedProductAsync(Guid productId)
    {
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message!);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products/unapproved-bulk-update";
        var response = await apiClient.PutAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                $"Onaysiz urun guncelleme hatasi: {response.StatusCode}",
                ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");
            return new ErrorResult($"Guncelleme hatasi: {response.StatusCode}");
        }

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            "Onaysiz urun Trendyol'da guncellendi",
            ProductActivityStatus.Success, marketplaceName: "Trendyol");

        return new SuccessResult("Onaysiz urun guncellendi.");
    }

    public async Task<IResult> DeleteProductAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .AsNoTracking()
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        var barcodeItems = product.ProductVariants
            .Select(v => new { barcode = v.Barcode })
            .ToList();

        var url = $"integration/product/sellers/{marketplace.SellerId}/products";
        var deleteBody = new { items = barcodeItems };
        var response = await apiClient.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol product delete failed. ProductId={ProductId}, Status={Status}, Body={Body}",
                productId, response.StatusCode, errorBody);

            await activityLogger.LogAsync(productId, ProductActivityType.Deleted,
                $"Trendyol silme hatası: {response.StatusCode}",
                ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");

            return new ErrorResult($"Trendyol silme hatası: {response.StatusCode}");
        }

        // Marketplace kaydını Failed olarak işaretle (arşivlenmiş/silinmiş)
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);
        if (pm is not null)
        {
            pm.Status = MarketplaceProductStatus.Failed;
            pm.StatusMessage = "Trendyol'dan silindi";
            await dbContext.SaveChangesAsync();
        }

        await activityLogger.LogAsync(productId, ProductActivityType.Deleted,
            "Ürün Trendyol'dan silindi",
            ProductActivityStatus.Success, marketplaceName: "Trendyol");

        return new SuccessResult("Ürün Trendyol'dan silindi.");
    }

    public async Task<IResult> UpdateApprovedContentAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);

        if (pm?.ContentId is null)
            return new ErrorResult("ContentId bulunamadı -- urun henuz onaylanmamis olabilir.");

        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message!);

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamis.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products/content-bulk-update";
        var response = await apiClient.PutAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                $"Onayli urun icerik guncelleme hatasi: {response.StatusCode}",
                ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");
            return new ErrorResult($"Icerik guncelleme hatasi: {response.StatusCode}");
        }

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Onayli urun icerigi Trendyol'da guncellendi (contentId={pm.ContentId})",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: pm.ContentId.ToString());

        return new SuccessResult("Onayli urun icerigi guncellendi.");
    }

    public async Task<IDataResult<TrendyolSendPreviewDto>> GetSendPreviewAsync(
        Guid productId,
        MarketplaceOverrideDetailDto? overrides)
    {
        const int TrendyolMarketPlaceId = 1;
        await using var context = await contextFactory.CreateDbContextAsync();

        var product = await context.MainProducts
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<TrendyolSendPreviewDto>(null!, "Ürün bulunamadı.");

        // 1. Kategori eşleştirmesini al
        var categoryMatch = await context.CategoryMarketPlaceMatches
            .FirstOrDefaultAsync(cm => cm.ApplicationCategoryId == product.CategoryId && cm.MarketPlaceId == TrendyolMarketPlaceId);

        if (categoryMatch is null)
            return new ErrorDataResult<TrendyolSendPreviewDto>(null!, "Kategori eşleştirmesi bulunamadı.");

        // 2. Marka eşleştirmesini al
        var brandMatch = await context.BrandMarketPlaceMatches
            .FirstOrDefaultAsync(bm => bm.ApplicationBrandId == product.BrandId && bm.MarketPlaceId == TrendyolMarketPlaceId);

        var trendyolBrandName = brandMatch?.MarketPlaceBrandId.ToString() ?? product.Brand?.Name ?? "Bilinmeyen Marka";

        // 3. Özellikler hazırla (boş tuple dn minimum için)
        var attributes = new List<TrendyolPreviewAttributeDto>();

        // 4. Varyantlar hazırla
        var variants = new List<TrendyolPreviewVariantDto>();
        foreach (var variant in product.ProductVariants)
        {
            // Basit attribute string (variant ID bazlı)
            var attrString = variant.ProductVariantAttributes.Count > 0
                ? string.Join(" / ", variant.ProductVariantAttributes.Select(a => a.CustomValue ?? a.CategoryAttributeValue ?? "—"))
                : "Varyant";

            // Override fiyat varsa kullan, yoksa mevcut fiyatı kullan
            var salePrice = overrides?.VariantOverrides
                .FirstOrDefault(vo => vo.ProductVariantId == variant.Id)
                ?.SalePriceOverride ?? variant.SalePrice;

            // Stok basit olarak 0 (DB schema'da stock ayrı tablada)
            var totalStock = 0;

            variants.Add(new TrendyolPreviewVariantDto(
                variant.Barcode ?? "—",
                attrString,
                variant.ListPrice,
                salePrice,
                totalStock
            ));
        }

        // 5. Başlık ve açıklamayı al
        var title = overrides?.TitleOverride ?? product.Title;
        var description = overrides?.DescriptionOverride ?? product.Description;

        var preview = new TrendyolSendPreviewDto(
            Title: title,
            BrandName: product.Brand?.Name ?? "Bilinmeyen Marka",
            TrendyolBrandName: trendyolBrandName,
            CategoryName: product.Category.Name,
            TrendyolCategoryName: categoryMatch.MarketPlaceCategoryId.ToString(),
            TrendyolCategoryId: categoryMatch.MarketPlaceCategoryId,
            Description: description,
            Attributes: attributes,
            Variants: variants
        );

        return new SuccessDataResult<TrendyolSendPreviewDto>(preview);
    }
}
