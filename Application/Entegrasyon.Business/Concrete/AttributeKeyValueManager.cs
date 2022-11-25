using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager
{
    public void ClearEmptyAttributes (ProductVariant pv)
    {
        pv.AttributeKeyValues = pv.AttributeKeyValues
            .Where(x => (x.AttributeValueId != null && x.AttributeValueId!=0) || !string.IsNullOrEmpty(x.CustomValue))
            .ToArray();
    }

}
