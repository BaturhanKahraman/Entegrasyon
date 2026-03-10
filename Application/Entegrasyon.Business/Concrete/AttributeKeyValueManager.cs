using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager(IntegrationDbContext attributeKeyValueDal) : IAttributeKeyValueManager
{
    public void ClearEmptyAttributes(Product product)
    {
        product.AttributeKeyValues = product.AttributeKeyValues
            .Where(x=>(x.AttributeValueId == 0 && !string.IsNullOrEmpty(x.CustomValue) || x.AttributeValueId>0))
            .ToList();
    }

    public async Task<IResult> ValidateAttributeKeyValues(IEnumerable<AttributeKeyValue> kv)
    {
        //todo
        throw new NotImplementedException();
        // var errorKeys = (await attributeKeyValueDal.ValidateKeyValues(kv)).ToList();
        // if(errorKeys.Any())
        //     return new ErrorResult(string.Join(' ',errorKeys) + " özellikleri eksiksiz doldurulmalıdır.");
        // return new SuccessResult();
    }

}
