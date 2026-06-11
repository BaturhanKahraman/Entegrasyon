using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.MVC.Features.Categories.ViewModels;

/// <summary>
/// Kategori özellik düzenleme sayfası (/categories/{id}/attributes) paneli (HTMX):
/// havuzdan ekle + bağlı olanı kaldır.
/// </summary>
public sealed class CategoryAttributePanelVm
{
    public required int CategoryId { get; init; }
    public required string CategoryName { get; init; }

    /// <summary>Tüm özellik havuzu (eklenebilir adaylar).</summary>
    public IReadOnlyList<CategoryAttribute> Pool { get; init; } = [];

    /// <summary>Kategoriye şu an bağlı özellikler (kaldırılabilir).</summary>
    public IReadOnlyList<CategoryAttributeDto> Attached { get; init; } = [];
}
