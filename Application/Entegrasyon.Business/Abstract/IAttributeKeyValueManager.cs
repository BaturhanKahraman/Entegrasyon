using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Abstract;

public interface IAttributeKeyValueManager
{
    void ClearEmptyAttributes(Product product);
}
