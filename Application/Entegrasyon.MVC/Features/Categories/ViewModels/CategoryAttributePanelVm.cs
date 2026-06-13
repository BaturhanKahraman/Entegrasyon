using System.Globalization;
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

    private static readonly StringComparer TurkishComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    /// <summary>
    /// Havuzdan eklenebilir adaylar: bağlı olanlar hariç, en çok kategoride
    /// kullanılan en üstte, eşitlikte TR alfabetik.
    /// </summary>
    public IReadOnlyList<CategoryAttribute> Addable
    {
        get
        {
            var attachedIds = Attached.Select(a => a.Id).ToHashSet();
            return Pool
                .Where(p => !attachedIds.Contains(p.Id))
                .OrderByDescending(p => p.Categories?.Count() ?? 0)
                .ThenBy(p => p.CategoryAttributeHumanized ?? p.CategoryAttributeKey, TurkishComparer)
                .ToList();
        }
    }
}
