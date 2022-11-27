using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Concrete;

public class AttributeKeyValueManager
{
    public void ClearEmptyAttributes (ProductVariant pv)
    {
        pv.AttributeKeyValues = pv.AttributeKeyValues
            .Where(x => x.AttributeValueId.HasValue ||
                        !string.IsNullOrEmpty(x.CustomValue)).ToList();
    }

}
