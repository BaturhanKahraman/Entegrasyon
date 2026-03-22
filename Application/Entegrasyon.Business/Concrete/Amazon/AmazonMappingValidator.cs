using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public class AmazonMappingValidator(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<AmazonMappingValidator> logger)
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;

    public virtual async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var errors = new List<string>();

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return new ErrorResult("Ürün bulunamadı.");

        if (!product.ProductVariants.Any())
            errors.Add("Ürünün en az bir varyantı olmalıdır.");

        if (product.BrandId == null)
            errors.Add("Ürünün markası belirlenmemiş.");

        foreach (var variant in product.ProductVariants)
        {
            if (string.IsNullOrWhiteSpace(variant.Barcode))
                errors.Add($"Varyant '{variant.Id}' için barkod (EAN) eksik.");
            if (!variant.Images.Any())
                errors.Add($"Varyant '{variant.Id}' için en az bir görsel gerekli.");
        }

        // Amazon'da kategori yerine product type eşleşmesi kontrol edilir
        var categoryMapped = await dbContext.CategoryMarketplaces
            .AnyAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == AmazonMpId);

        if (!categoryMapped)
            errors.Add("Ürünün kategorisi Amazon'a eşleştirilmemiş (product type tanımlı değil).");

        if (errors.Any())
        {
            var message = string.Join(" | ", errors);
            logger.LogWarning("Amazon mapping validation failed: {ProductId}: {Errors}", productId, message);
            return new ErrorResult(message);
        }

        return new SuccessResult();
    }
}
