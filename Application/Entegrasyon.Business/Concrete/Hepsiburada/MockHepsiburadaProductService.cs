using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Mock Hepsiburada ürün servisi — development ve test için.
/// Validation + mapping çalıştırır, gerçek API çağrısı yapmaz.
/// </summary>
public sealed class MockHepsiburadaProductService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHepsiburadaProductMapper productMapper,
    HepsiburadaMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<MockHepsiburadaProductService> logger) : IHepsiburadaProductService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        // 1. Validation (gerçek)
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            validationResult.Success ? "Hepsiburada mapping doğrulaması başarılı (MOCK)" : validationResult.Message!,
            validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
            marketplaceName: "Hepsiburada");

        if (!validationResult.Success)
            return new ErrorDataResult<string>(null!, validationResult.Message!);

        // 2. Mapping (gerçek)
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorDataResult<string>(null!, mapResult.Message!);

        // 3. Mock publish
        var mockTrackingId = $"mock-tracking-{Guid.NewGuid():N}";

        logger.LogInformation("[MOCK] HB publish: ProductId={ProductId}, TrackingId={TrackingId}, Items={Count}",
            productId, mockTrackingId, mapResult.Data?.Count ?? 0);
        logger.LogDebug("[MOCK] HB request body:\n{Body}",
            JsonSerializer.Serialize(mapResult.Data, new JsonSerializerOptions { WriteIndented = true }));

        // Update ProductMarketplace
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == HbMarketPlaceId);

        if (pm != null)
        {
            pm.BatchRequestId = mockTrackingId;
            pm.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            "Hepsiburada'ya ürün gönderildi (MOCK)",
            ProductActivityStatus.Success, null, "Hepsiburada", mockTrackingId);

        return new SuccessDataResult<string>(mockTrackingId);
    }

    public Task<IDataResult<List<HepsiburadaProductStatusItem>>> CheckProductStatusAsync(string trackingId)
    {
        logger.LogInformation("[MOCK] HB status check: {TrackingId}", trackingId);

        // Mock: hemen CREATED döndür
        var items = new List<HepsiburadaProductStatusItem>
        {
            new(
                MerchantSku: "MOCK-SKU",
                HbSku: $"HBCV{Guid.NewGuid():N}"[..18],
                Barcode: "1234567890123",
                ProductStatus: "CREATED",
                ProductName: "Mock Ürün",
                VariantGroupId: null,
                ImportStatus: "SUCCESS",
                ImportMessages: null,
                ValidationResults: null,
                MatchedHbProductInfo: null)
        };

        return Task.FromResult<IDataResult<List<HepsiburadaProductStatusItem>>>(
            new SuccessDataResult<List<HepsiburadaProductStatusItem>>(items));
    }

    public Task<IResult> ApprovePreMatchAsync(string merchantSku)
    {
        logger.LogInformation("[MOCK] HB approve-prematch: {MerchantSku}", merchantSku);
        return Task.FromResult<IResult>(new SuccessResult("PRE_MATCHED ürün onaylandı (MOCK)."));
    }
}
