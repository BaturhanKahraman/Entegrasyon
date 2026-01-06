using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAttributeKeyValueManager
{
    void ClearEmptyAttributes(Product product);
    Task<IResult> ValidateAttributeKeyValues(IEnumerable<AttributeKeyValue> kv);
}
