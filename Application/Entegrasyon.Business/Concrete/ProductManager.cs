using System.Linq.Expressions;
using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class ProductManager
{
    private readonly IMainProductDal _productDal;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IMapper _mapper;
    private readonly FluentValidator _validator;
    private readonly OfficeStockManager _officeStockManager;
    private readonly ImageManager _imageManager;
    private readonly AttributeKeyValueManager _attributeKeyValueManager;
    public ProductManager(IMainProductDal productDal, ApplicationLogManager applicationLogManager, IRandomGenerator randomGenerator, IMapper mapper, FluentValidator validator, OfficeStockManager officeStockManager, ImageManager imageManager, AttributeKeyValueManager attributeKeyValueManager)
    {
        _productDal = productDal;
        _applicationLogManager = applicationLogManager;
        _randomGenerator = randomGenerator;
        _mapper = mapper;
        _validator = validator;
        _officeStockManager = officeStockManager;
        _imageManager = imageManager;
        _attributeKeyValueManager = attributeKeyValueManager;
    }

    public async Task<IResult> AddProduct(AddProductDto dto)
    {
        await _applicationLogManager.AddLog("Ürün ekleme isteği geldi.", LogType.Product, LogAction.Add, dto);
        await _validator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(
            await _officeStockManager.CheckIfOfficeExists(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks.Select(y => y.BranchOfficeId)).ToArray()),
            _officeStockManager.CheckIfProductCountZero(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks).ToArray())
            );
        if (result != null)
            return result;
        foreach (var productVariantDto in dto.ProductVariants.Where(productVariantDto => string.IsNullOrEmpty(productVariantDto.Barcode)))
            productVariantDto.Barcode = _randomGenerator.GetRandomCode(13, true, false, false);
        var product = _mapper.Map<MainProduct>(dto);
        product.ProductVariants.ToList().ForEach(x => _attributeKeyValueManager.ClearEmptyAttributes(x));
        await _productDal.AddAsync(product);
        await _imageManager.AddProductImages(dto, product);
        await _applicationLogManager.AddLog("Ürün başarı ile eklendi", LogType.Product, LogAction.Add);
        return new SuccessResult();
    }

    public async Task<IResult> GetProductByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode))
            return new ErrorResult("Barkod boş olamaz.");
        var product = await _productDal.GetProductDetail(x => x.ProductVariants.Any(y => y.Barcode == barcode));
        if (product == null)
            return new ErrorResult("Barkoda ait ürün bulunamadı.");
        return new SuccessDataResult<ProductsDetailDto>(_mapper.Map<ProductsDetailDto>(product));
    }

    public async Task<IResult> UpdateProduct()
    {
        return new SuccessResult();
    }

    public async Task<IDataResult<ProductDetailDto>> GetProductDetailById(Guid productId)
    {
        var result = await _productDal.GetTransformedEntity(p =>
            new ProductDetailDto(p.Id,
                p.Title,
                p.Description,
                p.StockCode,
                p.Brand.Name,
                p.Category.Name,
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                p.ProductVariants.Select(pv => new ProductVariantDetailDto
                (pv.Id, pv.Barcode, pv.DimensionalWeight, pv.CurrencyType, pv.ListPrice, pv.SalePrice, pv.CostPrice, pv.VatRate,
                pv.Images.Select(img => img.Src).ToArray(),
                pv.BranchOfficeStocks.Select(stck => new StockDetailDto(stck.BranchOffice.Name, stck.CurrentStock, stck.SoldQuantity, stck.FirstTotalStock)),
                pv.AttributeKeyValues.Select(kv => new AttributeKeyValueDetailDto(kv.CategoryAttribute.CategoryAttributeKey,
                (kv.AttributeValueId != null || kv.AttributeValueId != 0) ? kv.AttributeValue.Name : kv.CustomValue))
                ))),expression:x=>x.Id==productId);
        return new SuccessDataResult<ProductDetailDto>(result);
    }
    public async Task<DataResult<Pageable<ProductsDetailDto>>> GetProductsDetailsPageable(GetProductPageableDto dto)
    {
        var orderTupleList = new List<(string, string)> { new("Id", "desc") };
        Expression<Func<MainProduct, bool>> expression = x => x.ProductVariants.Any(variant => variant.Barcode.Contains(dto.Barcode))
                                                         ||
                                                           x.SearchVector.Matches(EF.Functions.ToTsQuery(dto.FullTextSearchKey.ForFullTextSearch()));
        var result = await _productDal.GetPaginatedTransformedEntities(dto.PageIndex,
            dto.PageSize,
            x => new ProductsDetailDto(x.Id, x.Title, x.Description, x.StockCode, x.Brand.Name, x.Category.Name,
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.FirstTotalStock),
                x.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.SoldQuantity),
                x.ProductVariants.Count),
            orderTupleList, expression);
        return new SuccessDataResult<Pageable<ProductsDetailDto>>(result);
    }


}