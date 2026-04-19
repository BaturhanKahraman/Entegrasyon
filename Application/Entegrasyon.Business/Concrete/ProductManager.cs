using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Business.Concrete;

public class ProductManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    Entegrasyon.Business.Mappers.ProductMapper mapper,
    IFluentValidator validator,
    IOfficeStockManager officeStockManager,
    IAttributeKeyValueManager attributeKeyValueManager,
    IBarcodeService barcodeService,
    EventChannel<ProductAddedEvent> productAddedChannel,
    EventChannel<ProductUpdatedEvent> productUpdatedChannel,
    IMinioFileStorage minioFileStorage,
    ITenantContext tenantContext,
    IVariantNamingService namingService) : IProductService
{
    public async Task<IDataResult<Product>> AddProduct(AddProductDto dto)
    {
        await applicationLogManager.AddLog("Ürün ekleme isteği geldi.", LogType.Product, LogAction.Add, dto);
        await validator.ValidateAndThrowAsync(dto);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Business Rules
        var stockCodeConflict = !string.IsNullOrEmpty(dto.StockCode) &&
            await dbContext.MainProducts.AnyAsync(p => p.StockCode == dto.StockCode && !p.IsDeleted);
        if (stockCodeConflict)
            return new ErrorDataResult<Product>(null!, "Bu stok kodu zaten kullanılıyor. Lütfen farklı bir stok kodu girin.");

        // Leaf category check: category must have no children
        var hasChildren = await dbContext.Categories.AnyAsync(c => c.SuperCategoryId == dto.CategoryId && !c.IsDeleted);
        if (hasChildren)
            return new ErrorDataResult<Product>(null!, "Sadece alt kategorisi olmayan (yaprak) kategoriler secilebilir.");

        var check = LogicRunner.Run(
            officeStockManager.CheckIfProductCountZero(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks).ToArray())
        );
        if (check != null)
            return new ErrorDataResult<Product>(null!, check.Message!);

        foreach (var productVariantDto in dto.ProductVariants.Where(pv => string.IsNullOrEmpty(pv.Barcode)))
            productVariantDto.Barcode = await barcodeService.GenerateAsync();
        var product = mapper.MapToEntity(dto);

        // Kategori default VatRate doldurma: variant VatRate == 0 ise kategoriden al
        if (dto.CategoryId > 0)
        {
            var categoryDefaultVatRate = await dbContext.Categories
                .Where(c => c.Id == dto.CategoryId)
                .Select(c => c.DefaultVatRate)
                .FirstOrDefaultAsync();

            if (categoryDefaultVatRate.HasValue && categoryDefaultVatRate.Value > 0)
            {
                foreach (var variant in product.ProductVariants.Where(v => v.VatRate == 0))
                    variant.VatRate = categoryDefaultVatRate.Value;
            }
        }

        attributeKeyValueManager.ClearEmptyAttributes(product);
        foreach (var variant in product.ProductVariants)
            variant.Name = namingService.Compute(variant, product);
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ürün başarı ile eklendi", LogType.Product, LogAction.Add, "Product", product.Id.ToString());
        productAddedChannel.TryPublish(new ProductAddedEvent(product.Id, product.Title)
        {
            TenantId = tenantContext.TenantId
        });
        return new SuccessDataResult<Product>(product, Messages.ProductAdded);
    }

    public async Task<bool> IsStockCodeAvailableAsync(string? stockCode)
    {
        if (string.IsNullOrWhiteSpace(stockCode))
            return true;

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var exists = await dbContext.MainProducts
            .AnyAsync(p => p.StockCode == stockCode && !p.IsDeleted);
        return !exists;
    }

    public async Task<IResult> GetProductByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return new ErrorResult("Barkod boş olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var dto = await dbContext.MainProducts
            .Where(x => x.ProductVariants.Any(pv => pv.Barcode == barcode))
            .Select(x => new ProductsDetailDto(
                x.Id, x.Title, x.Description ?? "", x.StockCode ?? "",
                x.Brand!.Name!, x.Category!.Name!,
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                x.ProductVariants.Count()))
            .FirstOrDefaultAsync();
        if (dto == null)
            return new ErrorResult("Barkoda ait ürün bulunamadı.");
        return new SuccessDataResult<ProductsDetailDto>(dto);
    }

    public async Task<IDataResult<ProductEditPageDto>> GetProductEditPageData(Guid id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Where(p => p.Id == id)
            .Select(p => new ProductEditDetailDto(
                p.Id, p.Title, p.Description ?? "", p.StockCode ?? "",
                p.Season ?? "", p.Year ?? "",
                p.BrandId!.Value, p.CategoryId,
                p.ProductVariants.Select(pv => new ProductVariantEditDetailDto(
                    pv.Id, pv.DimensionalWeight, pv.CurrencyType, pv.Barcode ?? "", pv.Name,
                    pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.ECommercePrice, pv.VatRate,
                    pv.BranchOfficeStocks.Select(bos => new EditBranchOfficeStockDto(bos.BranchOfficeId, bos.FirstTotalStock)).ToList(),
                    pv.Images.Select(img => new EditableImageDto(img.Id, img.Src ?? "", img.IsMain, img.IsDeleted)).ToList(),
                    pv.ProductVariantAttributes
                        .Select(pva => new VariantAttributeDto(pva.CategoryAttributeValueId, pva.CategoryAttributeValue ?? "", pva.CustomValue ?? "", pva.IsVarianter, pva.IsSlicer))
                        .ToList()
                )).ToList(),
                p.AttributeKeyValues.Select(akv => new AttributeKeyValueDto(
                    akv.CategoryAttributeId,
                    akv.CategoryAttribute.CategoryAttributeKey!,
                    akv.AttributeValueId,
                    akv.AttributeValue != null ? akv.AttributeValue.Name! : string.Empty,
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
            .Select(b => new BranchSelectDto(b.Id, b.Name ?? ""))
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
        await applicationLogManager.AddLog("Ürün güncelleme isteği alındı.", LogType.Product, LogAction.Update, "Product", dto.Id.ToString(), dto);

        try { await validator.ValidateAndThrowAsync(dto); }
        catch (Exception ex) { return new ErrorResult(ex.Message); }

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var stockCodeConflict = await dbContext.MainProducts
            .AnyAsync(p => p.StockCode == dto.StockCode && p.Id != dto.Id && !p.IsDeleted);
        IResult stockCodeResult = stockCodeConflict
            ? new ErrorResult("Bu stok kodu başka bir ürün tarafından kullanılıyor.")
            : new SuccessResult();
        var check = LogicRunner.Run(stockCodeResult);
        if (check != null) return new ErrorResult(check.Message!);

        var product = await dbContext.MainProducts
            .AsTracking()
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images)
            .Include(p => p.AttributeKeyValues)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product is null)
            return new ErrorResult("Güncellenecek ürün bulunamadı.");

        // Snapshot old values for change history
        var changes = new Dictionary<string, ProductFieldChange>();
        void Track(string field, string? oldVal, string? newVal)
        {
            if (oldVal != newVal) changes[field] = new ProductFieldChange(oldVal, newVal);
        }
        Track("Title", product.Title, dto.Title);
        Track("Description", product.Description, dto.Description);
        Track("StockCode", product.StockCode, dto.StockCode);
        Track("Season", product.Season, dto.Season);
        Track("Year", product.Year, dto.Year);
        Track("BrandId", product.BrandId?.ToString(), dto.BrandId.ToString());
        Track("CategoryId", product.CategoryId.ToString(), dto.CategoryId.ToString());

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

        // Append change history
        if (changes.Count > 0)
        {
            var history = string.IsNullOrEmpty(product.ChangeHistory)
                ? new List<ProductChangeEntry>()
                : System.Text.Json.JsonSerializer.Deserialize<List<ProductChangeEntry>>(product.ChangeHistory) ?? [];
            history.Add(new ProductChangeEntry(DateTimeOffset.UtcNow, "admin", changes));
            product.ChangeHistory = System.Text.Json.JsonSerializer.Serialize(history);
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"'{product.Title}' ürünü güncellendi.", LogType.Product, LogAction.Update, "Product", product.Id.ToString());
        if (categoryChanged)
            await applicationLogManager.AddLog("Ürünün kategorisi değiştirildi, mevcut özellikler temizlendi.", LogType.Product, LogAction.Update);

        productUpdatedChannel.TryPublish(new ProductUpdatedEvent(product.Id, product.Title, categoryChanged)
        {
            TenantId = tenantContext.TenantId
        });

        return new SuccessResult("Ürün güncellendi.");
    }

    public async Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var raw = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                p.Title,
                Description = p.Description ?? "",
                StockCode = p.StockCode ?? "",
                Season = p.Season ?? "",
                Year = p.Year ?? "",
                p.BrandId,
                BrandName = p.Brand!.Name!,
                p.CategoryId,
                CategoryName = p.Category!.Name!,
                FirstTotalStock = p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                SoldQuantity = p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                Variants = p.ProductVariants.Select(pv => new
                {
                    pv.Id,
                    Barcode = pv.Barcode!,
                    pv.Name,
                    pv.DimensionalWeight,
                    pv.CurrencyType,
                    pv.ListPrice,
                    pv.SalePrice,
                    pv.CostPrice,
                    pv.ECommercePrice,
                    pv.VatRate,
                    ImageLinks = pv.Images.OrderBy(img => img.DisplayOrder)
                        .Select(img => img.StorageKey != null ? img.StorageKey + "_original.webp" : img.Src ?? "")
                        .ToArray(),
                    StockDetails = pv.BranchOfficeStocks.Select(stck => new StockDetailDto(stck.BranchOffice.Name!, stck.CurrentStock, stck.SoldQuantity, stck.FirstTotalStock)).ToList(),
                    RawAttrs = pv.ProductVariantAttributes.Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
                }).ToList(),
                AttributeKeyValues = p.AttributeKeyValues.Select(kv => new AttributeKeyValueDetailDto(
                    kv.CategoryAttribute.CategoryAttributeKey!,
                    kv.CategoryAttribute!.CategoryAttributeHumanized ?? kv.CategoryAttribute.CategoryAttributeKey ?? "",
                    kv.AttributeValueId.HasValue ? kv.AttributeValue!.Name! : kv.CustomValue ?? "")).ToList(),
                p.UpdatedAt,
                p.SeoTitle,
                p.SeoDescription,
                p.SeoSlug,
                p.SeoKeywords,
                p.IsPublished
            })
            .FirstOrDefaultAsync();

        if (raw is null) return new SuccessDataResult<ProductDetailDto>(null!);

        var result = new ProductDetailDto(
            raw.Id, raw.Title, raw.Description, raw.StockCode, raw.Season, raw.Year,
            raw.BrandId, raw.BrandName, raw.CategoryId, raw.CategoryName,
            raw.FirstTotalStock, raw.SoldQuantity,
            raw.Variants.Select(pv => new ProductVariantDetailDto(
                pv.Id, pv.Barcode,
                VariantNameExtensions.ResolveDisplayName(
                    pv.Name,
                    pv.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                    raw.Title),
                pv.DimensionalWeight, pv.CurrencyType, pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.ECommercePrice, pv.VatRate,
                pv.ImageLinks, pv.StockDetails
            )),
            raw.AttributeKeyValues,
            raw.UpdatedAt, raw.SeoTitle, raw.SeoDescription, raw.SeoSlug, raw.SeoKeywords, raw.IsPublished
        );

        // StorageKey → public URL dönüşümü (EF projection içinde yapılamaz)
        foreach (var variant in result.ProductVariantsDetails)
        {
            for (int i = 0; i < variant.imageLinks.Length; i++)
            {
                if (!string.IsNullOrEmpty(variant.imageLinks[i]))
                    variant.imageLinks[i] = minioFileStorage.GetPublicUrl(variant.imageLinks[i]);
            }
        }

        return new SuccessDataResult<ProductDetailDto>(result);
    }

    public async Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(SearchablePageDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.MainProducts.AsQueryable();
        if (!string.IsNullOrEmpty(dto.FullTextSearchKey))
            query = query.Where(x =>
                x.SearchVector.Matches(dto.FullTextSearchKey.ToFullTextSearchQuery()) ||
                x.ProductVariants.Any(pv => pv.Barcode!.Contains(dto.FullTextSearchKey)));

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .Select(x => new ProductsDetailDto(
                x.Id, x.Title, x.Description ?? "", x.StockCode ?? "", x.Brand!.Name!, x.Category!.Name!,
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                x.ProductVariants.Count())
            {
                FeaturedImageUrl = x.ProductVariants
                    .SelectMany(pv => pv.Images.Where(i => !i.IsDeleted))
                    .OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // StorageKey -> public URL
        for (int i = 0; i < items.Count; i++)
            if (!string.IsNullOrEmpty(items[i].FeaturedImageUrl))
                items[i].FeaturedImageUrl = minioFileStorage.GetPublicUrl(items[i].FeaturedImageUrl!);

        return new SuccessDataResult<Pageable<ProductsDetailDto>>(new Pageable<ProductsDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }

    public async Task<IDataResult<POSProductSearchResultDto>> SearchPOSProductsAsync(int branchOfficeId, string query, int limit = 10)
    {
        var result = new POSProductSearchResultDto();
        if (string.IsNullOrWhiteSpace(query))
            return new SuccessDataResult<POSProductSearchResultDto>(result);

        var trimmed = query.Trim();
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1) Barkod exact match — tek satırı işaretle, sepete doğrudan eklenebilsin
        //    Stoğu 0 olan varyant eşleşmesi yok sayılır (POS satış aracı — satılamayan ürün gösterilmez)
        var exactBarcodeRaw = await dbContext.ProductVariants
            .Where(pv => pv.Barcode == trimmed && !pv.IsDeleted && !pv.Product.IsDeleted &&
                pv.BranchOfficeStocks.Any(bs => bs.BranchOfficeId == branchOfficeId && bs.CurrentStock > 0))
            .Select(pv => new
            {
                pv.ProductId,
                VariantId = pv.Id,
                ProductTitle = pv.Product.Title,
                Barcode = pv.Barcode!,
                pv.SalePrice,
                pv.ListPrice,
                pv.VatRate,
                CurrentStock = pv.BranchOfficeStocks
                    .Where(bs => bs.BranchOfficeId == branchOfficeId)
                    .Sum(bs => bs.CurrentStock),
                ImageUrl = pv.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src ?? "")
                    .FirstOrDefault(),
                pv.Name,
                RawAttrs = pv.ProductVariantAttributes
                    .Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (exactBarcodeRaw is not null)
        {
            var exactBarcode = new POSProductSearchVariantDto
            {
                ProductId = exactBarcodeRaw.ProductId,
                VariantId = exactBarcodeRaw.VariantId,
                ProductTitle = exactBarcodeRaw.ProductTitle,
                Barcode = exactBarcodeRaw.Barcode,
                SalePrice = exactBarcodeRaw.SalePrice,
                ListPrice = exactBarcodeRaw.ListPrice,
                VatRate = exactBarcodeRaw.VatRate,
                CurrentStock = exactBarcodeRaw.CurrentStock,
                ImageUrl = exactBarcodeRaw.ImageUrl,
                DisplayName = VariantNameExtensions.ResolveDisplayName(
                    exactBarcodeRaw.Name,
                    exactBarcodeRaw.RawAttrs.Select((a, i) =>
                        new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                    exactBarcodeRaw.ProductTitle)
            };

            if (!string.IsNullOrEmpty(exactBarcode.ImageUrl))
                exactBarcode.ImageUrl = minioFileStorage.GetPublicUrl(exactBarcode.ImageUrl);
            result.ExactBarcodeMatch = exactBarcode;
        }

        // 2) Geniş arama — tsquery OR ILIKE (pg_trgm index) OR barkod contains
        var tsQuery = trimmed.ToFullTextSearchQuery();
        var likePattern = $"%{trimmed}%";

        var productItemsRaw = await dbContext.MainProducts
            .Where(p => !p.IsDeleted && (
                p.SearchVector.Matches(tsQuery) ||
                EF.Functions.ILike(p.Title, likePattern) ||
                (p.StockCode != null && EF.Functions.ILike(p.StockCode, likePattern)) ||
                p.ProductVariants.Any(pv => pv.Barcode != null && EF.Functions.ILike(pv.Barcode, likePattern))
            ))
            // Stoksuz ürünleri ele — en az bir varyantın seçili branşta pozitif stoğu olmalı
            .Where(p => p.ProductVariants.Any(pv => !pv.IsDeleted &&
                pv.BranchOfficeStocks.Any(bs => bs.BranchOfficeId == branchOfficeId && bs.CurrentStock > 0)))
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Title, trimmed))
            .Take(limit)
            .Select(p => new
            {
                ProductId = p.Id,
                Title = p.Title,
                StockCode = p.StockCode ?? "",
                BrandName = p.Brand!.Name ?? "",
                FeaturedImageUrl = p.ProductVariants
                    .SelectMany(pv => pv.Images.Where(i => !i.IsDeleted))
                    .OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                    .Select(i => i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src)
                    .FirstOrDefault(),
                Variants = p.ProductVariants
                    .Where(pv => !pv.IsDeleted &&
                        pv.BranchOfficeStocks.Any(bs => bs.BranchOfficeId == branchOfficeId && bs.CurrentStock > 0))
                    .Select(pv => new
                    {
                        VariantId = pv.Id,
                        Barcode = pv.Barcode ?? "",
                        pv.SalePrice,
                        pv.ListPrice,
                        pv.VatRate,
                        CurrentStock = pv.BranchOfficeStocks
                            .Where(bs => bs.BranchOfficeId == branchOfficeId)
                            .Sum(bs => bs.CurrentStock),
                        ImageUrl = pv.Images
                            .Where(i => !i.IsDeleted)
                            .OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                            .Select(i => i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src ?? "")
                            .FirstOrDefault(),
                        pv.Name,
                        RawAttrs = pv.ProductVariantAttributes
                            .Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        var productItems = productItemsRaw.Select(p => new POSProductSearchItemDto
        {
            ProductId = p.ProductId,
            Title = p.Title,
            StockCode = p.StockCode,
            BrandName = p.BrandName,
            FeaturedImageUrl = p.FeaturedImageUrl,
            Variants = p.Variants.Select(pv => new POSProductSearchVariantDto
            {
                ProductId = p.ProductId,
                VariantId = pv.VariantId,
                ProductTitle = p.Title,
                Barcode = pv.Barcode,
                SalePrice = pv.SalePrice,
                ListPrice = pv.ListPrice,
                VatRate = pv.VatRate,
                CurrentStock = pv.CurrentStock,
                ImageUrl = pv.ImageUrl,
                DisplayName = VariantNameExtensions.ResolveDisplayName(
                    pv.Name,
                    pv.RawAttrs.Select((a, i) =>
                        new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                    p.Title)
            }).ToList()
        }).ToList();

        // StorageKey → public URL dönüşümü (EF projection içinde yapılamaz)
        foreach (var p in productItems)
        {
            if (!string.IsNullOrEmpty(p.FeaturedImageUrl))
                p.FeaturedImageUrl = minioFileStorage.GetPublicUrl(p.FeaturedImageUrl);
            foreach (var v in p.Variants)
                if (!string.IsNullOrEmpty(v.ImageUrl))
                    v.ImageUrl = minioFileStorage.GetPublicUrl(v.ImageUrl);
        }

        result.Products = productItems;
        return new SuccessDataResult<POSProductSearchResultDto>(result);
    }

    public async Task<IResult> SoftDeleteProduct(Guid id)
    {
        await applicationLogManager.AddLog("Ürün silme isteği alındı.", LogType.Product, LogAction.Delete, "Product", id.ToString(), new { id });

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return new ErrorResult("Silinecek ürün bulunamadı.");
        product.IsDeleted = true;
        product.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog($"'{product.Title}' ürünü silindi.", LogType.Product, LogAction.Delete, "Product", product.Id.ToString());
        return new SuccessResult("Ürün silindi.");
    }

    public async Task<int> GetProductCountByCategoryId(int categoryId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.MainProducts.CountAsync(p => p.CategoryId == categoryId);
    }

    public async Task<bool> HasSoldProductsInCategory(int categoryId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.SaleItems.AnyAsync(si => si.ProductVariant.Product.CategoryId == categoryId);
    }

    public async Task<IDataResult<Product>> GetProductBySeoSlugAsync(string slug)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Include(p => p.Brand)
            .Include(p => p.Category).ThenInclude(c => c.SuperCategory)
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.ProductVariantAttributes)
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images.Where(i => !i.IsDeleted))
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.BranchOfficeStocks)
            .Include(p => p.AttributeKeyValues).ThenInclude(akv => akv.CategoryAttribute)
            .Include(p => p.AttributeKeyValues).ThenInclude(akv => akv.AttributeValue)
            .FirstOrDefaultAsync(p => p.SeoSlug == slug && !p.IsDeleted);

        if (product is null)
            return new ErrorDataResult<Product>(null!, "Ürün bulunamadı.");

        return new SuccessDataResult<Product>(product);
    }

    public async Task<IDataResult<Pageable<StorefrontProductCardDto>>> GetStorefrontProductsAsync(StorefrontCatalogQuery query)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var q = dbContext.MainProducts.Where(p => !p.IsDeleted);

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.BrandId.HasValue)
            q = q.Where(p => p.BrandId == query.BrandId.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
            q = q.Where(p =>
                p.SearchVector.Matches(query.SearchQuery.ToFullTextSearchQuery()) ||
                p.ProductVariants.Any(pv => pv.Barcode != null && pv.Barcode.Contains(query.SearchQuery)));

        if (query.MinPrice.HasValue)
            q = q.Where(p => p.ProductVariants.Any(pv => pv.SalePrice >= query.MinPrice.Value));

        if (query.MaxPrice.HasValue)
            q = q.Where(p => p.ProductVariants.Any(pv => pv.SalePrice <= query.MaxPrice.Value));

        int total = await q.CountAsync();

        q = query.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => q.OrderBy(p => p.ProductVariants.Min(pv => pv.SalePrice)),
            "price_desc" => q.OrderByDescending(p => p.ProductVariants.Max(pv => pv.SalePrice)),
            "name_asc" => q.OrderBy(p => p.Title),
            "name_desc" => q.OrderByDescending(p => p.Title),
            "bestseller" => q.OrderByDescending(p => p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(s => s.SoldQuantity)),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new StorefrontProductCardDto(
                p.Id,
                p.Title,
                p.SeoSlug,
                p.ProductVariants
                    .SelectMany(pv => pv.Images.Where(i => !i.IsDeleted))
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src)
                    .FirstOrDefault(),
                p.ProductVariants.Min(pv => pv.SalePrice),
                p.ProductVariants.Max(pv => pv.SalePrice),
                p.ProductVariants.Max(pv => pv.ListPrice) > p.ProductVariants.Max(pv => pv.SalePrice)
                    ? p.ProductVariants.Max(pv => pv.ListPrice)
                    : (decimal?)null,
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(s => s.CurrentStock),
                p.Brand != null ? p.Brand.Name : null,
                p.Category.Name,
                p.CreatedAt > DateTimeOffset.UtcNow.AddDays(-30)
            ))
            .ToListAsync();

        // StorageKey -> public URL conversion
        for (int i = 0; i < items.Count; i++)
        {
            if (!string.IsNullOrEmpty(items[i].ImageUrl))
                items[i] = items[i] with { ImageUrl = minioFileStorage.GetPublicUrl(items[i].ImageUrl!) };
        }

        return new SuccessDataResult<Pageable<StorefrontProductCardDto>>(
            new Pageable<StorefrontProductCardDto>(items, query.Page, query.PageSize, total));
    }

    public async Task<IDataResult<List<StorefrontProductCardDto>>> GetNewProductsAsync(int count)
    {
        var result = await GetStorefrontProductsAsync(new StorefrontCatalogQuery(PageSize: count));
        if (!result.Success)
            return new ErrorDataResult<List<StorefrontProductCardDto>>(new List<StorefrontProductCardDto>(), result.Message!);
        return new SuccessDataResult<List<StorefrontProductCardDto>>(result.Data.Items.ToList());
    }

    public async Task<IDataResult<List<StorefrontProductCardDto>>> GetBestSellersAsync(int count)
    {
        var result = await GetStorefrontProductsAsync(new StorefrontCatalogQuery(SortBy: "bestseller", PageSize: count));
        if (!result.Success)
            return new ErrorDataResult<List<StorefrontProductCardDto>>(new List<StorefrontProductCardDto>(), result.Message!);
        return new SuccessDataResult<List<StorefrontProductCardDto>>(result.Data.Items.ToList());
    }

    public async Task<IDataResult<List<StorefrontSearchSuggestionDto>>> GetSearchSuggestionsAsync(string query, int maxResults = 8)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return new SuccessDataResult<List<StorefrontSearchSuggestionDto>>(new List<StorefrontSearchSuggestionDto>());

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var term = query.Trim();

        var products = await dbContext.MainProducts
            .Where(p => !p.IsDeleted && EF.Functions.ILike(p.Title, $"%{term}%"))
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new StorefrontSearchSuggestionDto(
                p.Title,
                $"/urun/{p.SeoSlug}",
                "Ürün",
                p.ProductVariants
                    .SelectMany(pv => pv.Images.Where(i => !i.IsDeleted))
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src)
                    .FirstOrDefault()))
            .ToListAsync();

        var categories = await dbContext.Categories
            .Where(c => !c.IsDeleted && EF.Functions.ILike(c.Name, $"%{term}%"))
            .Take(2)
            .Select(c => new StorefrontSearchSuggestionDto(c.Name, $"/kategori/{c.SeoSlug}", "Kategori", null))
            .ToListAsync();

        var brands = await dbContext.Brands
            .Where(b => !b.IsDeleted && EF.Functions.ILike(b.Name, $"%{term}%"))
            .Take(1)
            .Select(b => new StorefrontSearchSuggestionDto(b.Name, $"/marka/{b.SeoSlug}", "Marka", null))
            .ToListAsync();

        var suggestions = products.Concat(categories).Concat(brands).Take(maxResults).ToList();

        // StorageKey -> public URL conversion for product images
        for (int i = 0; i < suggestions.Count; i++)
        {
            if (!string.IsNullOrEmpty(suggestions[i].ImageUrl))
                suggestions[i] = suggestions[i] with { ImageUrl = minioFileStorage.GetPublicUrl(suggestions[i].ImageUrl!) };
        }

        return new SuccessDataResult<List<StorefrontSearchSuggestionDto>>(suggestions);
    }

    public async Task<IDataResult<StorefrontProductDetailDto>> GetStorefrontProductDetailAsync(string seoSlug)
    {
        var productResult = await GetProductBySeoSlugAsync(seoSlug);
        if (!productResult.Success || productResult.Data is null)
            return new ErrorDataResult<StorefrontProductDetailDto>(null!, productResult.Message!);

        var p = productResult.Data;

        var variants = p.ProductVariants.Select(pv => new StorefrontVariantDto(
            pv.Id,
            pv.Barcode,
            pv.ListPrice,
            pv.SalePrice,
            pv.BranchOfficeStocks.Sum(s => s.CurrentStock),
            pv.ProductVariantAttributes
                .Select(a => new StorefrontVariantAttributeDto(
                    a.CategoryAttributeValue ?? "",
                    a.CustomValue ?? a.CategoryAttributeValue ?? ""))
                .ToList(),
            pv.Images.OrderBy(i => i.DisplayOrder)
                .Select(i =>
                {
                    var key = i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src;
                    return !string.IsNullOrEmpty(key) ? minioFileStorage.GetPublicUrl(key) : "";
                })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList()
        )).ToList();

        var attributes = p.AttributeKeyValues
            .Where(akv => akv.CategoryAttribute is not null)
            .Select(akv => new StorefrontAttributeDto(
                akv.CategoryAttribute.CategoryAttributeKey ?? "",
                akv.CategoryAttribute.CategoryAttributeHumanized ?? akv.CategoryAttribute.CategoryAttributeKey ?? "",
                akv.AttributeValueId.HasValue && akv.AttributeValue is not null
                    ? akv.AttributeValue.Name ?? ""
                    : akv.CustomValue ?? ""))
            .ToList();

        // Build breadcrumbs
        var breadcrumbs = new List<BreadcrumbItemDto> { new("Ana Sayfa", "/") };
        if (p.Category.SuperCategory is not null)
            breadcrumbs.Add(new BreadcrumbItemDto(p.Category.SuperCategory.Name, $"/kategori/{p.Category.SuperCategory.SeoSlug}"));
        breadcrumbs.Add(new BreadcrumbItemDto(p.Category.Name, $"/kategori/{p.Category.SeoSlug}"));
        breadcrumbs.Add(new BreadcrumbItemDto(p.Title, $"/urun/{p.SeoSlug}"));

        var detail = new StorefrontProductDetailDto(
            p.Id, p.Title, p.Description, p.StockCode,
            p.SeoSlug, p.SeoTitle, p.SeoDescription,
            p.Brand?.Name, p.Brand?.SeoSlug,
            p.Category.Name, p.Category.SeoSlug, p.CategoryId,
            p.ProductVariants.Min(pv => pv.SalePrice),
            p.ProductVariants.Max(pv => pv.SalePrice),
            variants, attributes, breadcrumbs);

        return new SuccessDataResult<StorefrontProductDetailDto>(detail);
    }

    public async Task<IDataResult<List<StorefrontProductDetailDto>>> GetProductsByIdsAsync(List<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
            return new SuccessDataResult<List<StorefrontProductDetailDto>>(new List<StorefrontProductDetailDto>());

        var distinctIds = ids.Distinct().Take(10).ToList();
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var products = await dbContext.MainProducts
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.BranchOfficeStocks)
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.Images)
            .Include(p => p.ProductVariants).ThenInclude(pv => pv.ProductVariantAttributes)
            .Include(p => p.AttributeKeyValues).ThenInclude(akv => akv.CategoryAttribute)
            .Include(p => p.AttributeKeyValues).ThenInclude(akv => akv.AttributeValue)
            .Include(p => p.Category).ThenInclude(c => c.SuperCategory)
            .Include(p => p.Brand)
            .AsNoTracking()
            .Where(p => distinctIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync();

        var results = products.Select(p =>
        {
            var variants = p.ProductVariants.Select(pv => new StorefrontVariantDto(
                pv.Id, pv.Barcode, pv.ListPrice, pv.SalePrice,
                pv.BranchOfficeStocks.Sum(s => s.CurrentStock),
                pv.ProductVariantAttributes
                    .Select(a => new StorefrontVariantAttributeDto(
                        a.CategoryAttributeValue ?? "", a.CustomValue ?? a.CategoryAttributeValue ?? ""))
                    .ToList(),
                pv.Images.OrderBy(i => i.DisplayOrder)
                    .Select(i =>
                    {
                        var key = i.StorageKey != null ? i.StorageKey + "_original.webp" : i.Src;
                        return !string.IsNullOrEmpty(key) ? minioFileStorage.GetPublicUrl(key) : "";
                    })
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList()
            )).ToList();

            var attributes = p.AttributeKeyValues
                .Where(akv => akv.CategoryAttribute is not null)
                .Select(akv => new StorefrontAttributeDto(
                    akv.CategoryAttribute.CategoryAttributeKey ?? "",
                    akv.CategoryAttribute.CategoryAttributeHumanized ?? akv.CategoryAttribute.CategoryAttributeKey ?? "",
                    akv.AttributeValueId.HasValue && akv.AttributeValue is not null
                        ? akv.AttributeValue.Name ?? ""
                        : akv.CustomValue ?? ""))
                .ToList();

            var breadcrumbs = new List<BreadcrumbItemDto> { new("Ana Sayfa", "/") };
            if (p.Category?.SuperCategory is not null)
                breadcrumbs.Add(new BreadcrumbItemDto(p.Category.SuperCategory.Name, $"/kategori/{p.Category.SuperCategory.SeoSlug}"));
            if (p.Category is not null)
                breadcrumbs.Add(new BreadcrumbItemDto(p.Category.Name, $"/kategori/{p.Category.SeoSlug}"));
            breadcrumbs.Add(new BreadcrumbItemDto(p.Title, $"/urun/{p.SeoSlug}"));

            return new StorefrontProductDetailDto(
                p.Id, p.Title, p.Description, p.StockCode,
                p.SeoSlug, p.SeoTitle, p.SeoDescription,
                p.Brand?.Name, p.Brand?.SeoSlug,
                p.Category?.Name ?? "", p.Category?.SeoSlug, p.CategoryId,
                p.ProductVariants.Any() ? p.ProductVariants.Min(pv => pv.SalePrice) : 0,
                p.ProductVariants.Any() ? p.ProductVariants.Max(pv => pv.SalePrice) : 0,
                variants, attributes, breadcrumbs);
        }).ToList();

        // Preserve original order from ids
        var ordered = distinctIds
            .Select(id => results.FirstOrDefault(r => r.Id == id))
            .Where(r => r is not null)
            .Cast<StorefrontProductDetailDto>()
            .ToList();

        return new SuccessDataResult<List<StorefrontProductDetailDto>>(ordered);
    }

    public async Task<IDataResult<List<BrandFilterDto>>> GetBrandsForCategoryAsync(int categoryId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var brands = await dbContext.MainProducts
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.CategoryId == categoryId && p.BrandId.HasValue)
            .GroupBy(p => new { p.BrandId, p.Brand!.Name })
            .Select(g => new BrandFilterDto(g.Key.BrandId!.Value, g.Key.Name, g.Count()))
            .OrderBy(b => b.BrandName)
            .ToListAsync();

        return new SuccessDataResult<List<BrandFilterDto>>(brands);
    }

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

    public async Task<IResult> UpdateStoreSettings(
        Guid productId,
        string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords,
        Dictionary<Guid, decimal> variantECommercePrices)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        // Slug unique kontrolü
        if (!string.IsNullOrWhiteSpace(seoSlug))
        {
            var slugExists = await dbContext.MainProducts
                .AnyAsync(p => p.SeoSlug == seoSlug && p.Id != productId && !p.IsDeleted);
            if (slugExists)
                return new ErrorResult("Bu SEO URL zaten başka bir ürün tarafından kullanılıyor.");
        }

        product.SeoTitle = seoTitle;
        product.SeoDescription = seoDescription;
        product.SeoSlug = seoSlug;
        product.SeoKeywords = seoKeywords;

        foreach (var variant in product.ProductVariants)
        {
            if (variantECommercePrices.TryGetValue(variant.Id, out var ecomPrice))
                variant.ECommercePrice = ecomPrice;
        }

        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Mağaza ayarları güncellendi.", LogType.Product, LogAction.Update, "Product", productId.ToString());
        return new SuccessResult("Mağaza ayarları kaydedildi.");
    }

    public async Task<IResult> PublishProduct(
        Guid productId,
        string? seoTitle, string? seoDescription, string? seoSlug, string? seoKeywords,
        Dictionary<Guid, decimal> variantECommercePrices)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorResult("Ürün bulunamadı.");

        // Slug unique kontrolü
        if (!string.IsNullOrWhiteSpace(seoSlug))
        {
            var slugExists = await dbContext.MainProducts
                .AnyAsync(p => p.SeoSlug == seoSlug && p.Id != productId && !p.IsDeleted);
            if (slugExists)
                return new ErrorResult("Bu SEO URL zaten başka bir ürün tarafından kullanılıyor.");
        }

        product.SeoTitle = seoTitle;
        product.SeoDescription = seoDescription;
        product.SeoSlug = seoSlug;
        product.SeoKeywords = seoKeywords;
        product.IsPublished = true;

        foreach (var variant in product.ProductVariants)
        {
            if (variantECommercePrices.TryGetValue(variant.Id, out var ecomPrice))
                variant.ECommercePrice = ecomPrice;
        }

        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ürün mağazada yayınlandı.", LogType.Product, LogAction.Update, "Product", productId.ToString());
        return new SuccessResult("Ürün mağazada yayınlandı.");
    }

    public async Task<Dictionary<Guid, string>> GetVariantAttributeNamesAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var variants = await dbContext.Set<ProductVariant>()
            .Where(v => v.ProductId == productId)
            .Select(v => new
            {
                v.Id,
                Attrs = v.ProductVariantAttributes
                    .Select(a => a.CategoryAttributeValue)
                    .ToList()
            })
            .ToListAsync();

        return variants.ToDictionary(
            v => v.Id,
            v => string.Join(" - ", v.Attrs));
    }
}
