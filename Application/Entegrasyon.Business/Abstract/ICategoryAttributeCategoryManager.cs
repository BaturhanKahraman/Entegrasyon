using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAttributeCategoryManager
{
    Task<IResult> AddCategoryAttributeForCategory(int catId, IEnumerable<AddCategoryAttributeDto> dto);

    /// <summary>
    /// Kategoriye bağlı tek bir özelliği (junction satırını) kaldırır.
    /// Pipeline: Validation (geçerli id'ler) → BusinessRules (kategori + bağ var mı) → Execution (soft-delete junction).
    /// Var olmayan / zaten kaldırılmış bağ için hata döner.
    /// </summary>
    Task<IResult> RemoveCategoryAttributeFromCategory(int catId, int categoryAttributeId);
}
