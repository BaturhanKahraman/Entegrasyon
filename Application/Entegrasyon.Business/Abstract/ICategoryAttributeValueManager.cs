using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAttributeValueManager
{
    Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id);
    Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds);
    Task<int> GetOrCreate(int categoryAttributeId, string rawName);

    /// <summary>Değeri yeniden adlandırır. Sadece (id, categoryAttributeId) eşleşen aktif kaydı günceller
    /// (IDOR koruması). Bulunamazsa false döner.</summary>
    Task<bool> UpdateName(int id, int categoryAttributeId, string newName);

    /// <summary>Değeri soft-delete eder. Sadece (id, categoryAttributeId) eşleşen aktif kaydı siler
    /// (IDOR koruması). Bulunamazsa false döner.</summary>
    Task<bool> SoftDelete(int id, int categoryAttributeId);
}
