using Entegrasyon.Entity.Products;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager() : IAttributeKeyValueManager
{
    public void ClearEmptyAttributes(Product product)
    {
        product.AttributeKeyValues = product.AttributeKeyValues
            .Where(x=>(x.AttributeValueId == 0 && !string.IsNullOrEmpty(x.CustomValue) || x.AttributeValueId>0))
            .ToList();
    }

}
