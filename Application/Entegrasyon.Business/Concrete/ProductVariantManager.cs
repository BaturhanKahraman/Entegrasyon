using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class ProductVariantManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator fluentValidator,
    IBarcodeService barcodeService,
    IImageManager imageManager,
    EventChannel<ProductUpdatedEvent> productUpdatedChannel,
    ITenantContext tenantContext,
    IVariantNamingService namingService) : IProductVariantManager
{
    public async Task<ProductVariant> GetById(Guid id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return (await dbContext.ProductVariants.FirstOrDefaultAsync(x => x.Id == id))!;
    }

    public async Task<string> GetLastProductVariantBarcode()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return (await dbContext.ProductVariants.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Barcode != null))?.Barcode ?? "";
    }

    public async Task<List<string>> GetAllVariantsBarcodes()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.ProductVariants.Select(x => x.Barcode!).ToListAsync();
    }

    public async Task<IDataResult<ProductVariantSaleSearchDto>> GetProductVariantByBarcode(string barcode)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var raw = await dbContext.ProductVariants
            .Where(x => x.Barcode == barcode)
            .Select(x => new
            {
                x.Id,
                ProductTitle = x.Product.Title,
                Image = x.Images.FirstOrDefault(img => img.IsMain)!.Src ?? x.Images.FirstOrDefault()!.Src ?? "",
                x.VatRate,
                x.ListPrice,
                x.SalePrice,
                x.CostPrice,
                Stock = x.BranchOfficeStocks.Sum(z => z.CurrentStock),
                CategoryName = x.Product.Category.Name,
                x.Name,
                RawAttrs = x.ProductVariantAttributes.Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
            })
            .FirstOrDefaultAsync();
        if (raw == null)
            return new ErrorDataResult<ProductVariantSaleSearchDto>(null!, Messages.ProductVariantNotFound);

        var displayName = VariantNameExtensions.ResolveDisplayName(
            raw.Name,
            raw.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
            raw.ProductTitle);

        var result = new ProductVariantSaleSearchDto(
            raw.Id, raw.ProductTitle, displayName, raw.Image, raw.VatRate, raw.ListPrice, raw.SalePrice, raw.CostPrice, raw.Stock, raw.CategoryName);

        return new SuccessDataResult<ProductVariantSaleSearchDto>(result, Messages.ProductVariantGettingSuccessful);
    }

    public async Task<IResult> GetProductVariantsBySearchText(string fullTextSearch)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var raw = await dbContext.ProductVariants
            .Where(x => x.BranchOfficeStocks.Sum(stck => stck.CurrentStock) > 0 &&
                (x.Product.SearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery()))
                 || x.Barcode == fullTextSearch))
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                ProductTitle = x.Product.Title,
                Image = x.Images.FirstOrDefault(img => img.IsMain)!.Src ?? x.Images.FirstOrDefault()!.Src ?? "",
                x.VatRate,
                x.ListPrice,
                x.SalePrice,
                x.CostPrice,
                Stock = x.BranchOfficeStocks.Sum(z => z.CurrentStock),
                CategoryName = x.Product.Category.Name,
                x.Name,
                RawAttrs = x.ProductVariantAttributes.Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
            })
            .ToListAsync();

        var result = raw.Select(r => new ProductVariantSaleSearchDto(
            r.Id, r.ProductTitle,
            VariantNameExtensions.ResolveDisplayName(
                r.Name,
                r.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                r.ProductTitle),
            r.Image, r.VatRate, r.ListPrice, r.SalePrice, r.CostPrice, r.Stock, r.CategoryName
        )).ToList();

        return new SuccessDataResult<List<ProductVariantSaleSearchDto>>(result, Messages.ProductVariantGettingSuccessful);
    }

    // ── GetVariantEditDetail ─────────────────────────────────────────

    public async Task<IDataResult<ProductVariantEditDetailDto>> GetVariantEditDetail(Guid variantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Include(v => v.BranchOfficeStocks)
            .Include(v => v.Images)
            .Include(v => v.ProductVariantAttributes)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant is null)
            return new ErrorDataResult<ProductVariantEditDetailDto>(null!, "Varyant bulunamadı.");

        var dto = new ProductVariantEditDetailDto(
            variant.Id,
            variant.DimensionalWeight,
            variant.CurrencyType,
            variant.Barcode ?? "",
            variant.Name,
            variant.ListPrice,
            variant.SalePrice,
            variant.CostPrice,
            variant.ECommercePrice,
            variant.VatRate,
            variant.BranchOfficeStocks.Select(s =>
                new EditBranchOfficeStockDto(s.BranchOfficeId, s.CurrentStock)).ToList(),
            variant.Images.Where(i => !i.IsDeleted).Select(i =>
                new EditableImageDto(i.Id, i.Src ?? "", i.IsMain, i.IsDeleted)).ToList(),
            variant.ProductVariantAttributes.Select(a =>
                new Entity.Dtos.Attributes.VariantAttributeDto(
                    a.CategoryAttributeValueId,
                    a.CategoryAttributeValue ?? "",
                    a.CustomValue ?? "",
                    a.IsVarianter,
                    a.IsSlicer)).ToList()
        );

        return new SuccessDataResult<ProductVariantEditDetailDto>(dto);
    }

    // ── AddVariant ───────────────────────────────────────────────────

    public async Task<IResult> AddVariant(Guid productId, AddProductVariantDto dto)
    {
        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var product = await dbContext.MainProducts.AsTracking()
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.ProductVariantAttributes)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        // 3. Execution
        await applicationLogManager.AddLog("Varyant ekleniyor.", LogType.Product, LogAction.Add, "Product", productId.ToString());

        var barcode = string.IsNullOrWhiteSpace(dto.Barcode)
            ? await barcodeService.GenerateAsync()
            : dto.Barcode;

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Barcode = barcode,
            ListPrice = dto.ListPrice ?? 0,
            SalePrice = dto.SalePrice ?? 0,
            CostPrice = dto.CostPrice ?? 0,
            ECommercePrice = dto.SalePrice ?? 0,
            VatRate = dto.VatRate ?? 0,
            DimensionalWeight = dto.DimensionalWeight ?? 0,
            CurrencyType = dto.CurrencyType ?? "TRY",
            BranchOfficeStocks = dto.BranchOfficeStocks.Select(s => new BranchOfficeStock
            {
                BranchOfficeId = s.BranchOfficeId,
                FirstTotalStock = s.FirstTotalStock,
                CurrentStock = s.FirstTotalStock
            }).ToList()
        };

        variant.Name = namingService.Compute(variant, product);

        // Kardeş varyant reconcile: Mevcut variant'ların Name'i product.Title'a eşitse
        // (yani fallback ile atanmışsa, user override değil) yeniden hesapla.
        // Bu, tek-variant'tan multi-variant'a geçişte "Basic Tshirt" gibi stale isimlerin
        // ilgili attribute'lara göre yeniden üretilmesini sağlar.
        foreach (var sibling in product.ProductVariants.Where(v => v.Name == product.Title))
            sibling.Name = namingService.Compute(sibling, product);

        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();

        // Sync trigger: Product.UpdatedAt dokunarak OutOfSync yap
        PublishProductUpdated(product.Id, product.Title);

        await applicationLogManager.AddLog($"Varyant eklendi. Barkod: {barcode}", LogType.Product, LogAction.Add, "Product", productId.ToString());
        return new SuccessResult("Varyant başarıyla eklendi.");
    }

    // ── UpdateVariant ────────────────────────────────────────────────

    public async Task<IResult> UpdateVariant(EditProductVariantDto dto)
    {
        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var variant = await dbContext.ProductVariants.AsTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == dto.Id);
        if (variant is null)
            return new ErrorResult("Güncellenecek varyant bulunamadı.");

        // 3. Execution
        await applicationLogManager.AddLog("Varyant güncelleniyor.", LogType.Product, LogAction.Update, "Product", variant.ProductId.ToString());

        variant.ListPrice = dto.ListPrice;
        variant.SalePrice = dto.SalePrice;
        variant.CostPrice = dto.CostPrice;
        variant.ECommercePrice = dto.ECommercePrice;
        variant.VatRate = dto.VatRate;
        variant.DimensionalWeight = dto.DimensionalWeight;
        variant.CurrencyType = dto.CurrencyType;
        // Kullanıcı elle Name verdi → sakla; boşsa attribute'lardan yeniden hesapla.
        variant.Name = !string.IsNullOrWhiteSpace(dto.Name)
            ? dto.Name.Trim()
            : namingService.Compute(variant, variant.Product);

        await dbContext.SaveChangesAsync();

        // Sync trigger
        PublishProductUpdated(variant.ProductId, variant.Product.Title);

        await applicationLogManager.AddLog("Varyant güncellendi.", LogType.Product, LogAction.Update, "Product", variant.ProductId.ToString());
        return new SuccessResult("Varyant başarıyla güncellendi.");
    }

    // ── SoftDeleteVariant ────────────────────────────────────────────

    public async Task<IResult> SoftDeleteVariant(Guid variantId)
    {
        await applicationLogManager.AddLog("Varyant silme isteği alındı.", LogType.Product, LogAction.Delete, "ProductVariant", variantId.ToString(), new { variantId });

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var variant = await dbContext.ProductVariants.AsTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);
        if (variant is null)
            return new ErrorResult("Silinecek varyant bulunamadı.");

        variant.IsDeleted = true;
        variant.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        // Kullanılmayan resimleri temizle
        await imageManager.SoftDeleteVariantImages(variantId);

        // Sync trigger
        PublishProductUpdated(variant.ProductId, variant.Product.Title);

        await applicationLogManager.AddLog($"Varyant silindi. Barkod: {variant.Barcode}", LogType.Product, LogAction.Delete, "Product", variant.ProductId.ToString());
        return new SuccessResult("Varyant silindi.");
    }

    // ── GetProductVariantImages ─────────────────────────────────────

    public async Task<List<(Guid VariantId, string Barcode, List<(int ImageId, string Src, bool IsMain)> Images)>> GetProductVariantImages(Guid productId, Guid? excludeVariantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var variants = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId && !v.IsDeleted && v.Id != (excludeVariantId ?? Guid.Empty))
            .Include(v => v.Images)
            .Select(v => new
            {
                v.Id,
                Barcode = v.Barcode ?? "",
                Images = v.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new { i.Id, Src = i.Src ?? "", i.IsMain })
                    .ToList()
            })
            .Where(v => v.Images.Any())
            .ToListAsync();

        return variants.Select(v => (
            v.Id,
            v.Barcode,
            v.Images.Select(i => (i.Id, i.Src, i.IsMain)).ToList()
        )).ToList();
    }

    // ── GetVariantDetailPage ─────────────────────────────────────────

    public async Task<IDataResult<VariantDetailPageDto>> GetVariantDetailPage(Guid variantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.BranchOfficeStocks).ThenInclude(s => s.BranchOffice)
            .Include(v => v.Images)
            .Include(v => v.ProductVariantAttributes)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant is null)
            return new ErrorDataResult<VariantDetailPageDto>(null!, "Varyant bulunamadı.");

        // Marketplace sync bilgisi (Product seviyesinde)
        var marketplaceRecords = await dbContext.ProductMarketplaces
            .AsNoTracking()
            .Where(pm => pm.ProductId == variant.ProductId)
            .Include(pm => pm.MarketPlace)
            .ToListAsync();

        // Variant-level overrides
        var variantOverrides = await dbContext.ProductVariantMarketplaceOverrides
            .AsNoTracking()
            .Where(o => o.ProductVariantId == variantId)
            .ToListAsync();

        var marketplaces = marketplaceRecords.Select(pm =>
        {
            var state = MapSyncState(pm, variant.Product.UpdatedAt);
            var ovr = variantOverrides.FirstOrDefault(o => o.ProductMarketplaceId == pm.Id);
            return new MarketplaceVariantInfo(
                pm.MarketPlaceId,
                pm.MarketPlace.Name,
                state,
                SyncBadgeClass(state),
                pm.LastSyncedAt,
                pm.StatusMessage,
                ovr?.ListPriceOverride,
                ovr?.SalePriceOverride
            );
        }).ToList();

        var displayName = VariantNameExtensions.ResolveDisplayName(
            variant.Name,
            variant.ProductVariantAttributes.Select((a, i) =>
                new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
            variant.Product.Title);

        var dto = new VariantDetailPageDto(
            variant.ProductId,
            variant.Product.Title,
            displayName,
            variant.Id,
            variant.Barcode ?? "",
            variant.ListPrice,
            variant.SalePrice,
            variant.CostPrice,
            variant.ECommercePrice,
            variant.VatRate,
            variant.DimensionalWeight,
            variant.CurrencyType,
            variant.CreatedAt,
            variant.UpdatedAt,
            variant.BranchOfficeStocks.Select(s =>
                new StockDetailDto(s.BranchOffice?.Name ?? "", s.CurrentStock, s.SoldQuantity, s.FirstTotalStock)).ToList(),
            variant.Images.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).Select(i =>
                new VariantImageInfo(i.Id, i.Src ?? "", i.IsMain)).ToList(),
            variant.ProductVariantAttributes.Select(a =>
                new VariantAttributeInfo(
                    a.CategoryAttributeValue ?? a.CustomValue ?? "",
                    a.CategoryAttributeValue ?? a.CustomValue ?? "",
                    a.IsVarianter,
                    a.IsSlicer)).ToList(),
            marketplaces
        );

        return new SuccessDataResult<VariantDetailPageDto>(dto);
    }

    // ── Private Helpers ──────────────────────────────────────────────

    private void PublishProductUpdated(Guid productId, string productTitle)
    {
        productUpdatedChannel.TryPublish(new ProductUpdatedEvent(productId, productTitle, false)
        {
            TenantId = tenantContext.TenantId
        });
    }

    private static MarketplaceSyncState MapSyncState(ProductMarketplace? marketplace, DateTimeOffset productUpdatedAt)
    {
        if (marketplace is null) return MarketplaceSyncState.NeverSynced;
        return marketplace.Status switch
        {
            MarketplaceProductStatus.Pending when marketplace.BatchRequestId is null => MarketplaceSyncState.Waiting,
            MarketplaceProductStatus.Pending => MarketplaceSyncState.Processing,
            MarketplaceProductStatus.Published when productUpdatedAt > marketplace.LastSyncedAt => MarketplaceSyncState.OutOfSync,
            MarketplaceProductStatus.Published => MarketplaceSyncState.Synced,
            MarketplaceProductStatus.Failed => MarketplaceSyncState.Failed,
            MarketplaceProductStatus.Rejected => MarketplaceSyncState.Rejected,
            _ => MarketplaceSyncState.NeverSynced
        };
    }

    private static string SyncBadgeClass(MarketplaceSyncState state) => state switch
    {
        MarketplaceSyncState.Synced => "bg-green-lt",
        MarketplaceSyncState.Waiting => "bg-warning-lt",
        MarketplaceSyncState.Processing => "bg-info-lt",
        MarketplaceSyncState.OutOfSync => "bg-orange-lt",
        MarketplaceSyncState.Failed => "bg-danger-lt",
        MarketplaceSyncState.Rejected => "bg-danger-lt",
        _ => "bg-secondary-lt"
    };
}
