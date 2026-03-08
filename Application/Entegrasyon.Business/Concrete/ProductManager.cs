using System.Linq.Expressions;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.DTO;
using Shared.Entity;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;
    private readonly IFluentValidator _validator;
    private readonly IOfficeStockManager _officeStockManager;
    private readonly IAttributeKeyValueManager _attributeKeyValueManager;
    private readonly IBarcodeService _barcodeService;

    public ProductManager(
        IntegrationDbContext dbContext,
        IApplicationLogManager applicationLogManager,
        IMapper mapper,
        IFluentValidator validator,
        IOfficeStockManager officeStockManager,
        IAttributeKeyValueManager attributeKeyValueManager,
        IBarcodeService barcodeService)
    {
        _dbContext = dbContext;
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
        _validator = validator;
        _officeStockManager = officeStockManager;
        _attributeKeyValueManager = attributeKeyValueManager;
        _barcodeService = barcodeService;
    }

    public async Task<IDataResult<Product>> AddProduct(AddProductDto dto)
    {
        await _applicationLogManager.AddLog("Ürün ekleme isteği geldi.", LogType.Product, LogAction.Add, dto);
        await _validator.ValidateAndThrowAsync(dto);
        var check = LogicRunner.Run(
            _officeStockManager.CheckIfProductCountZero(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks).ToArray())
        );
        if (check != null)
            return new ErrorDataResult<Product>(null!, check.Message);
        foreach (var productVariantDto in dto.ProductVariants.Where(pv => string.IsNullOrEmpty(pv.Barcode)))
            productVariantDto.Barcode = await _barcodeService.GenerateAsync();
        var product = _mapper.Map<Product>(dto);
        _attributeKeyValueManager.ClearEmptyAttributes(product);
        _dbContext.MainProducts.Add(product);
        await _dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Ürün başarı ile eklendi", LogType.Product, LogAction.Add);
        return new SuccessDataResult<Product>(product, Messages.ProductAdded);
    }

    public async Task<IResult> GetProductByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return new ErrorResult("Barkod boş olamaz.");
        var product = await _dbContext.MainProducts
            .FirstOrDefaultAsync(x => x.ProductVariants.Any(pv => pv.Barcode == barcode));
        if (product == null)
            return new ErrorResult("Barkoda ait ürün bulunamadı.");
        return new SuccessDataResult<ProductsDetailDto>(_mapper.Map<ProductsDetailDto>(product));
    }

    public async Task<IResult> UpdateProduct(ProductEditDetailDto dto)
    {
        await _applicationLogManager.AddLog("Ürün güncelleniyor.", LogType.Product, LogAction.Update, dto);
        var product = _mapper.Map<Product>(dto);
        _dbContext.MainProducts.Update(product);
        await _dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Ürün güncellendi.", LogType.Product, LogAction.Update, dto);
        return new SuccessResult();
    }

    public async Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId)
    {
        var result = await _dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailDto(
                p.Id, p.Title, p.Description, p.StockCode, p.Brand.Name, p.Category.Name,
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                p.ProductVariants.Select(pv => new ProductVariantDetailDto(
                    pv.Id, pv.Barcode, pv.DimensionalWeight, pv.CurrencyType, pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.VatRate,
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
        var query = _dbContext.MainProducts.AsQueryable();
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

    public async Task<IDataResult<ProductEditDetailDto>> GetProductEditDetailById(Guid id)
    {
        var result = await _dbContext.MainProducts
            .Where(p => p.Id == id)
            .Select(p => new ProductEditDetailDto(
                p.Id, p.Title, p.Description, p.StockCode, p.BrandId!.Value, p.CategoryId,
                p.ProductVariants.Select(pv => new ProductVariantEditDetailDto(
                    pv.Id, pv.DimensionalWeight, pv.CurrencyType, pv.Barcode, pv.ListPrice,
                    pv.SalePrice, pv.CostPrice, pv.VatRate,
                    pv.BranchOfficeStocks.Select(bos => new EditBranchOfficeStockDto(bos.BranchOfficeId, bos.FirstTotalStock)).ToList(),
                    pv.Images.Select(img => new EditableImageDto(img.Id, img.Src, img.IsCoverImage, img.IsDeleted)).ToList(),
                    pv.ProductVariantAttributes
                        .Select(pva => new VariantAttributeDto(pva.CategoryAttributeValueId, pva.CategoryAttributeValue, pva.CustomValue, pva.IsVarianter, pva.IsSlicer))
                        .ToList()
                )).ToList(),
                p.AttributeKeyValues.Select(akv => new AttributeKeyValueDto(
                    akv.CategoryAttributeId,
                    akv.CategoryAttribute.CategoryAttributeKey,
                    akv.AttributeValueId,
                    akv.AttributeValue.Name,
                    akv.CategoryAttribute.Categories.FirstOrDefault(ca => ca.CategoryId == p.CategoryId).IsRequired,
                    akv.CustomValue)
                ).ToList()))
            .FirstOrDefaultAsync();
        return new SuccessDataResult<ProductEditDetailDto>(result);
    }

    public Task<int> GetProductCountByCategoryId(int categoryId) =>
        _dbContext.MainProducts.CountAsync(p => p.CategoryId == categoryId);
}
