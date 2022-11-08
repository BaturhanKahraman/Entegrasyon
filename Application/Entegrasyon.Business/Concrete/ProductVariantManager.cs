using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Concrete;

public class ProductVariantManager
{
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IProductVariantDal _productVariantDal;

    public ProductVariantManager(IProductVariantDal productVariantDal, ApplicationLogManager applicationLogManager)
    {
        _productVariantDal = productVariantDal;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<ProductVariant> GetById(Guid id)
    {
        return await _productVariantDal.GetAsync(x => x.Id == id);
    }
}