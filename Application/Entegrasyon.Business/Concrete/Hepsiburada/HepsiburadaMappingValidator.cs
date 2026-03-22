using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada'ya publish öncesi mapping doğrulaması.
/// Kategori eşleşmesi, barkod, görsel, zorunlu attribute kontrolleri.
/// </summary>
public class HepsiburadaMappingValidator(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<HepsiburadaMappingValidator> logger)
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    public virtual async Task<IResult> ValidateProductMappingsAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var errors = new List<string>();

        // 1. Ürün var mı?
        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return new ErrorResult("Ürün bulunamadı.");
        }

        // 2. Kategori eşleşmesi
        var categoryMapped = await dbContext.CategoryMarketplaces
            .AnyAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == HbMarketPlaceId);

        if (!categoryMapped)
        {
            errors.Add("Ürünün kategorisi Hepsiburada'ya eşleştirilmemiş.");
        }

        // 3. Marka bilgisi (HB'de attribute olarak gidiyor, brand match tablosu gerekmez)
        if (product.BrandId == null)
        {
            errors.Add("Ürünün markası belirlenmemiş.");
        }

        // 4. Varyant ve barkod kontrolü
        if (!product.ProductVariants.Any())
        {
            errors.Add("Ürünün en az bir varyantı olmalıdır.");
        }
        else
        {
            foreach (var variant in product.ProductVariants)
            {
                if (string.IsNullOrWhiteSpace(variant.Barcode))
                {
                    errors.Add($"Varyant '{variant.Id}' için barkod eksik.");
                }
                else if (variant.Barcode.Length != 13)
                {
                    errors.Add($"Varyant '{variant.Id}' barkodu EAN13 formatında değil (13 karakter olmalı).");
                }

                if (!variant.Images.Any())
                {
                    errors.Add($"Varyant '{variant.Id}' için en az bir görsel gerekli.");
                }
            }
        }

        // 5. Zorunlu attribute eşleşmeleri
        var requiredAttrs = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Select(cac => new { cac.CategoryAttributeId, cac.CategoryAttribute.CategoryAttributeHumanized })
            .ToListAsync();

        var mappedAttrIds = await dbContext.CategoryAttributeMarketPlaceMatches
            .Where(m => m.MarketPlaceId == HbMarketPlaceId &&
                        m.MarketPlaceCategoryAttributeExternalId != null)
            .Select(m => m.ApplicationCategoryAttributeId)
            .ToListAsync();

        var unmappedCount = requiredAttrs.Count(ra => !mappedAttrIds.Contains(ra.CategoryAttributeId));
        if (unmappedCount > 0)
        {
            errors.Add($"{unmappedCount} zorunlu özellik Hepsiburada'ya eşleştirilmemiş.");
        }

        if (errors.Any())
        {
            var message = string.Join(" | ", errors);
            logger.LogWarning("HB mapping validation failed for product {ProductId}: {Errors}", productId, message);
            return new ErrorResult(message);
        }

        return new SuccessResult();
    }
}
