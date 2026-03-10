using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class ProductVariantManager : IProductVariantManager
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IApplicationLogManager _applicationLogManager;

    public ProductVariantManager(IntegrationDbContext dbContext, IApplicationLogManager applicationLogManager)
    {
        _dbContext = dbContext;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<ProductVariant> GetById(Guid id) =>
        await _dbContext.ProductVariants.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<string> GetLastProductVariantBarcode() =>
        (await _dbContext.ProductVariants.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Barcode != null))?.Barcode;

    public Task<List<string>> GetAllVariantsBarcodes() =>
        _dbContext.ProductVariants.Select(x => x.Barcode).ToListAsync();

    public async Task<IDataResult<ProductVariantSaleSearchDto>> GetProductVariantByBarcode(string barcode)
    {
        var result = await _dbContext.ProductVariants
            .Where(x => x.Barcode == barcode)
            .Select(x => new ProductVariantSaleSearchDto(
                x.Id, x.Product.Title,
                x.Images.FirstOrDefault(img => img.IsCoverImage).Src ?? x.Images.FirstOrDefault().Src,
                x.VatRate, x.ListPrice, x.SalePrice, x.CostPrice,
                x.BranchOfficeStocks.Sum(z => z.CurrentStock),
                x.Product.Category.Name))
            .FirstOrDefaultAsync();
        if (result == null)
            return new ErrorDataResult<ProductVariantSaleSearchDto>(null, Messages.ProductVariantNotFound);
        return new SuccessDataResult<ProductVariantSaleSearchDto>(result, Messages.ProductVariantGettingSuccessful);
    }

    public async Task<IResult> GetProductVariantsBySearchText(string fullTextSearch)
    {
        var result = await _dbContext.ProductVariants
            .Where(x => x.BranchOfficeStocks.Sum(stck => stck.CurrentStock) > 0 &&
                (x.Product.SearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery()))
                 || x.Barcode == fullTextSearch))
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt)
            .Select(x => new ProductVariantSaleSearchDto(
                x.Id, x.Product.Title,
                x.Images.FirstOrDefault(img => img.IsCoverImage).Src ?? x.Images.FirstOrDefault().Src,
                x.VatRate, x.ListPrice, x.SalePrice, x.CostPrice,
                x.BranchOfficeStocks.Sum(z => z.CurrentStock),
                x.Product.Category.Name))
            .ToListAsync();
        if (result == null)
            return new ErrorResult(Messages.ProductVariantNotFound);
        return new SuccessDataResult<List<ProductVariantSaleSearchDto>>(result, Messages.ProductVariantGettingSuccessful);
    }
}
