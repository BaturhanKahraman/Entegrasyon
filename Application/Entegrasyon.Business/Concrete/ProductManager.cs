using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Business.Concrete;

public class ProductManager(
    IntegrationDbContext dbContext,
    IApplicationLogManager applicationLogManager,
    IMapper mapper,
    IFluentValidator validator,
    IOfficeStockManager officeStockManager,
    IAttributeKeyValueManager attributeKeyValueManager,
    IBarcodeService barcodeService,
    EventChannel<ProductAddedEvent> productAddedChannel,
    EventChannel<ProductUpdatedEvent> productUpdatedChannel) : IProductService
{
    public async Task<IDataResult<Product>> AddProduct(AddProductDto dto)
    {
        await applicationLogManager.AddLog("Ürün ekleme isteği geldi.", LogType.Product, LogAction.Add, dto);
        await validator.ValidateAndThrowAsync(dto);
        var check = LogicRunner.Run(
            officeStockManager.CheckIfProductCountZero(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks).ToArray())
        );
        if (check != null)
            return new ErrorDataResult<Product>(null!, check.Message);
        foreach (var productVariantDto in dto.ProductVariants.Where(pv => string.IsNullOrEmpty(pv.Barcode)))
            productVariantDto.Barcode = await barcodeService.GenerateAsync();
        var product = mapper.Map<Product>(dto);
        attributeKeyValueManager.ClearEmptyAttributes(product);
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ürün başarı ile eklendi", LogType.Product, LogAction.Add);
        productAddedChannel.TryPublish(new ProductAddedEvent(product.Id, product.Title));
        return new SuccessDataResult<Product>(product, Messages.ProductAdded);
    }

    public async Task<IResult> GetProductByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return new ErrorResult("Barkod boş olamaz.");
        var product = await dbContext.MainProducts
            .FirstOrDefaultAsync(x => x.ProductVariants.Any(pv => pv.Barcode == barcode));
        if (product == null)
            return new ErrorResult("Barkoda ait ürün bulunamadı.");
        return new SuccessDataResult<ProductsDetailDto>(mapper.Map<ProductsDetailDto>(product));
    }

    public async Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id)
    {
        var product = await dbContext.MainProducts
            .Where(p => p.Id == id)
            .Select(p => new ProductEditDetailDto(
                p.Id, p.Title, p.Description, p.StockCode,
                p.Season, p.Year,
                p.BrandId!.Value, p.CategoryId,
                p.ProductVariants.Select(pv => new ProductVariantEditDetailDto(
                    pv.Id, pv.DimensionalWeight, pv.CurrencyType, pv.Barcode,
                    pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.ECommercePrice, pv.VatRate,
                    pv.BranchOfficeStocks.Select(bos => new EditBranchOfficeStockDto(bos.BranchOfficeId, bos.FirstTotalStock)).ToList(),
                    pv.Images.Select(img => new EditableImageDto(img.Id, img.Src, img.IsMain, img.IsDeleted)).ToList(),
                    pv.ProductVariantAttributes
                        .Select(pva => new VariantAttributeDto(pva.CategoryAttributeValueId, pva.CategoryAttributeValue, pva.CustomValue, pva.IsVarianter, pva.IsSlicer))
                        .ToList()
                )).ToList(),
                p.AttributeKeyValues.Select(akv => new AttributeKeyValueDto(
                    akv.CategoryAttributeId,
                    akv.CategoryAttribute.CategoryAttributeKey,
                    akv.AttributeValueId,
                    akv.AttributeValue != null ? akv.AttributeValue.Name : string.Empty,
                    akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId) == null ? false : akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId)!.IsRequired,
                    akv.CustomValue ?? string.Empty)
                ).ToList()))
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorDataResult<ProductEditPageDto>(null!, "Ürün bulunamadı.");

        var brands = await dbContext.Brands
            .Where(b => !b.IsDeleted)
            .Select(b => new BrandListDetailDto(b.Id, b.CreatedAt, b.Name, 0))
            .ToListAsync();

        var categories = await dbContext.Categories
            .Where(c => !c.IsDeleted && !c.SubCategories.Any())
            .Select(c => new CategorySelectDto(c.Id, c.Name))
            .ToListAsync();

        var branches = await dbContext.BranchOffices
            .Where(b => !b.IsDeleted)
            .Select(b => new BranchSelectDto(b.Id, b.Name))
            .ToListAsync();

        var marketplace = await dbContext.ProductMarketplaces
            .Where(pm => pm.ProductId == id && pm.MarketPlaceId == 1)
            .FirstOrDefaultAsync();

        var updatedAt = await dbContext.MainProducts
            .Where(p => p.Id == id)
            .Select(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        var syncStatus = BuildSyncStatus(marketplace, updatedAt);

        return new SuccessDataResult<ProductEditPageDto>(
            new ProductEditPageDto(product, brands, categories, branches, syncStatus));
    }

    public async Task<IResult> UpdateProduct(EditProductDto dto)
    {
        await applicationLogManager.AddLog("Ürün güncelleme isteği alındı.", LogType.Product, LogAction.Update, dto);

        try { await validator.ValidateAndThrowAsync(dto); }
        catch (Exception ex) { return new ErrorResult(ex.Message); }

        var stockCodeConflict = await dbContext.MainProducts
            .AnyAsync(p => p.StockCode == dto.StockCode && p.Id != dto.Id && !p.IsDeleted);
        IResult stockCodeResult = stockCodeConflict
            ? new ErrorResult("Bu stok kodu başka bir ürün tarafından kullanılıyor.")
            : new SuccessResult();
        var check = LogicRunner.Run(stockCodeResult);
        if (check != null) return new ErrorResult(check.Message);

        var product = await dbContext.MainProducts
            .AsTracking()
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images)
            .Include(p => p.AttributeKeyValues)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product is null)
            return new ErrorResult("Güncellenecek ürün bulunamadı.");

        product.Title = dto.Title;
        product.Description = dto.Description;
        product.StockCode = dto.StockCode;
        product.Season = dto.Season;
        product.Year = dto.Year;
        product.BrandId = dto.BrandId;

        bool categoryChanged = product.CategoryId != dto.CategoryId;
        if (categoryChanged)
        {
            product.CategoryId = dto.CategoryId;
        }

        dbContext.AttributeKeyValues.RemoveRange(product.AttributeKeyValues);
        product.AttributeKeyValues.Clear();

        foreach (var akv in dto.AttributeKeyValues.Where(a => a.AttributeValueId.HasValue || !string.IsNullOrWhiteSpace(a.CustomValue)))
        {
            product.AttributeKeyValues.Add(new AttributeKeyValue
            {
                CategoryAttributeId = akv.CategoryAttributeId,
                AttributeValueId = akv.AttributeValueId,
                CustomValue = akv.CustomValue
            });
        }

        foreach (var variantDto in dto.Variants)
        {
            var variant = product.ProductVariants.FirstOrDefault(pv => pv.Id == variantDto.Id);
            if (variant is null) continue;
            variant.ListPrice = variantDto.ListPrice;
            variant.SalePrice = variantDto.SalePrice;
            variant.CostPrice = variantDto.CostPrice;
            variant.ECommercePrice = variantDto.ECommercePrice;
            variant.DimensionalWeight = variantDto.DimensionalWeight;
            variant.VatRate = variantDto.VatRate;
            variant.CurrencyType = variantDto.CurrencyType;
        }

        foreach (var imageId in dto.DeletedImageIds)
        {
            var image = product.ProductVariants
                .SelectMany(pv => pv.Images)
                .FirstOrDefault(img => img.Id == imageId);
            if (image is not null)
            {
                image.IsDeleted = true;
                image.DeletedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"'{product.Title}' ürünü güncellendi.", LogType.Product, LogAction.Update);
        if (categoryChanged)
            await applicationLogManager.AddLog("Ürünün kategorisi değiştirildi, mevcut özellikler temizlendi.", LogType.Product, LogAction.Update);

        productUpdatedChannel.TryPublish(new ProductUpdatedEvent(product.Id, product.Title, categoryChanged));

        return new SuccessResult("Ürün güncellendi.");
    }

    public async Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId)
    {
        var result = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailDto(
                p.Id, p.Title, p.Description, p.StockCode, p.Season, p.Year, p.Brand.Name, p.Category.Name,
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                p.ProductVariants.Select(pv => new ProductVariantDetailDto(
                    pv.Id, pv.Barcode, pv.DimensionalWeight, pv.CurrencyType, pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.ECommercePrice, pv.VatRate,
                    pv.Images.Select(img => img.Src).ToArray(),
                    pv.BranchOfficeStocks.Select(stck => new StockDetailDto(stck.BranchOffice.Name, stck.CurrentStock, stck.SoldQuantity, stck.FirstTotalStock))
                )),
                p.AttributeKeyValues.Select(kv => new AttributeKeyValueDetailDto(
                    kv.CategoryAttribute.CategoryAttributeKey,
                    kv.AttributeValueId.HasValue ? kv.AttributeValue.Name : kv.CustomValue))
            ))
            .FirstOrDefaultAsync();
        return new SuccessDataResult<ProductDetailDto>(result);
    }

    public async Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto)
    {
        var query = dbContext.MainProducts.AsQueryable();
        if (!string.IsNullOrEmpty(dto.FullTextSearchKey))
            query = query.Where(x =>
                x.SearchVector.Matches(dto.FullTextSearchKey.ToFullTextSearchQuery()) ||
                x.ProductVariants.Any(pv => pv.Barcode.Contains(dto.FullTextSearchKey)));

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .Select(x => new ProductsDetailDto(
                x.Id, x.Title, x.Description, x.StockCode, x.Brand.Name, x.Category.Name,
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                x.ProductVariants.Count()))
            .ToListAsync();

        return new SuccessDataResult<Pageable<ProductsDetailDto>>(new Pageable<ProductsDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }

    public async Task<IResult> SoftDeleteProduct(Guid id)
    {
        await applicationLogManager.AddLog("Ürün silme isteği alındı.", LogType.Product, LogAction.Delete, new { id });
        var product = await dbContext.MainProducts.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return new ErrorResult("Silinecek ürün bulunamadı.");
        product.IsDeleted = true;
        product.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog($"'{product.Title}' ürünü silindi.", LogType.Product, LogAction.Delete);
        return new SuccessResult("Ürün silindi.");
    }

    public Task<int> GetProductCountByCategoryId(int categoryId) =>
        dbContext.MainProducts.CountAsync(p => p.CategoryId == categoryId);

    public Task<bool> HasSoldProductsInCategory(int categoryId) =>
        dbContext.SaleItems.AnyAsync(si => si.ProductVariant.Product.CategoryId == categoryId);

    private static MarketplaceSyncStatusDto BuildSyncStatus(ProductMarketplace? marketplace, DateTimeOffset? productUpdatedAt)
    {
        if (marketplace is null)
            return new MarketplaceSyncStatusDto(MarketplaceSyncState.NeverSynced, null, null, null);

        var state = marketplace.Status switch
        {
            MarketplaceProductStatus.Pending when marketplace.BatchRequestId is null => MarketplaceSyncState.Waiting,
            MarketplaceProductStatus.Pending => MarketplaceSyncState.Processing,
            MarketplaceProductStatus.Published when productUpdatedAt > marketplace.LastSyncedAt => MarketplaceSyncState.OutOfSync,
            MarketplaceProductStatus.Published => MarketplaceSyncState.Synced,
            MarketplaceProductStatus.Failed => MarketplaceSyncState.Failed,
            MarketplaceProductStatus.Rejected => MarketplaceSyncState.Rejected,
            _ => MarketplaceSyncState.NeverSynced
        };

        return new MarketplaceSyncStatusDto(state, marketplace.LastSyncedAt, marketplace.BatchRequestId, marketplace.StatusMessage);
    }
}
