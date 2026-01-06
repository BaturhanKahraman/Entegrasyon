using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAttributeValueManager
{
    Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id);
    Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds);
}
