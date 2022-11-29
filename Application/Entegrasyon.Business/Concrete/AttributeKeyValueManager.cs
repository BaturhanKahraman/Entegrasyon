using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager
{
    private readonly IAttributeKeyValueDal _attributeKeyValueDal;
    public void ClearEmptyAttributes (ProductVariant pv)
    {
        pv.AttributeKeyValues = pv.AttributeKeyValues
            .Where(x => x.AttributeValueId.HasValue ||
                        !string.IsNullOrEmpty(x.CustomValue)).ToList();
    }

    public async Task<IResult> ValidateAttributeKeyValues(IEnumerable<AttributeKeyValue> kv)
    {
        var errorKeys =await _attributeKeyValueDal.ValidateKeyValues(kv);
        if (errorKeys.Any())
            return new ErrorResult(string.Join(' ',errorKeys,Environment.NewLine,"Belirtilen ürün özellikleri zorunlu olmalıdır."));
        return new SuccessResult();
    }
    
}
