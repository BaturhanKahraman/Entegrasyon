using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager
{
    private readonly IAttributeKeyValueDal _attributeKeyValueDal;

    public AttributeKeyValueManager(IAttributeKeyValueDal attributeKeyValueDal)
    {
        _attributeKeyValueDal = attributeKeyValueDal;
    }

    public void ClearEmptyAttributes (Product product)
    {
        product.AttributeKeyValues = product.AttributeKeyValues
            .Where(x => x.AttributeValueId.HasValue ||
                        !string.IsNullOrEmpty(x.CustomValue)).ToList();
    }

    public async Task<IResult> ValidateAttributeKeyValues(IEnumerable<AttributeKeyValue> kv)
    {
        var errorKeys =(await _attributeKeyValueDal.ValidateKeyValues(kv)).ToList();
        if (errorKeys.Any())
            return new ErrorResult(string.Join(' ',errorKeys)+ " özellikleri eksiksiz doldurulmalıdır.");
        return new SuccessResult();
    }
    
}
