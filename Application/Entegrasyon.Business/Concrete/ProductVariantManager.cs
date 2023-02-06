using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class ProductVariantManager
{
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IProductVariantDal _productVariantDal;

    public ProductVariantManager(IProductVariantDal productVariantDal,ApplicationLogManager applicationLogManager)
    {
        _productVariantDal = productVariantDal;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<ProductVariant> GetById(Guid id)
    {
        return await _productVariantDal.GetAsync(x => x.Id == id);
    }

    public async Task<string> GetLastProductVariantBarcode() => (await _productVariantDal.Table
        .AsNoTracking().OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Barcode != null))?.Barcode;

    public Task<List<string>> GetAllVariantsBarcodes() => _productVariantDal.GetTransformedEntitiesAsync(x => x.Barcode);

    public async Task<IDataResult<ProductVariantSaleSearchDto>> GetProductVariantByBarcode(string barcode)
    {
        var result = await _productVariantDal.GetTransformedEntity(x => new ProductVariantSaleSearchDto(
            x.Id,x.Product.Title,
            x.Images.FirstOrDefault(img => img.IsCoverImage).Src ?? x.Images.FirstOrDefault().Src,
            x.VatRate,
            x.ListPrice,
            x.SalePrice,
            x.CostPrice,
            x.BranchOfficeStocks.Sum(z => z.CurrentStock),
            x.Product.Category.Name
            ),x => string.Equals(x.Barcode,barcode));
        if(result == null)
            return new ErrorDataResult<ProductVariantSaleSearchDto>(null,Messages.ProductVariantNotFound);
        return new SuccessDataResult<ProductVariantSaleSearchDto>(result,Messages.ProductVariantGettingSuccessful);
    }
    public async Task<IResult> GetProductVariantsBySearchText(string fullTextSearch)
    {
        var orderTuple = new List<(string, string)>
        {
            new ("CreatedAt","Desc"),new("UpdatedAt","Desc")
        };
        var result = await _productVariantDal.GetTransformedEntitiesAsync(x => new ProductVariantSaleSearchDto(
            x.Id,x.Product.Title,
            x.Images.FirstOrDefault(img => img.IsCoverImage).Src ?? x.Images.FirstOrDefault().Src,
            x.VatRate,
            x.ListPrice,
            x.SalePrice,
            x.CostPrice,
            x.BranchOfficeStocks.Sum(z => z.CurrentStock),
            x.Product.Category.Name
        ),orderTuple,
            x => x.BranchOfficeStocks.Sum(stck=>stck.CurrentStock)>0 &&
                 (x.Product.SearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery())) 
                  || string.Equals(x.Barcode,fullTextSearch)
                  )
            );
        if(result == null)
            return new ErrorResult(Messages.ProductVariantNotFound);
        return new SuccessDataResult<List<ProductVariantSaleSearchDto>>(result,Messages.ProductVariantGettingSuccessful);
    }
}