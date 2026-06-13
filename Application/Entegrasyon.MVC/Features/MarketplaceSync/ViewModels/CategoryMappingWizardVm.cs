using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

/// <summary>
/// Kategori ↔ pazaryeri eşleme wizard'ı (modal yerine sayfa). Üç adım: kategori eşle →
/// zorunlu özellikler → değerler. Eşleme durumuna göre aktif adım belirlenir.
/// </summary>
public class CategoryMappingWizardVm
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public int MarketPlaceId { get; set; } = 1;
    public string MarketPlaceName { get; set; } = string.Empty;
    public List<MarketPlace> MarketPlaces { get; set; } = [];

    public bool IsMapped { get; set; }
    public int? MarketPlaceCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }

    /// <summary>Eşli ise zorunlu/varianter özellik tamlık durumu (UI progress'i besler).</summary>
    public CategoryMatchValidationResultDto? Validation { get; set; }

    public string? ReturnUrl { get; set; }

    // ── Hesaplanan adım durumu ──
    public int ActiveStep => !IsMapped ? 1 : (RequiredComplete ? 3 : 2);
    public bool RequiredComplete =>
        Validation is not null && Validation.MappedRequiredAttributes >= Validation.TotalRequiredAttributes;
    public int RequiredPercent =>
        Validation is { TotalRequiredAttributes: > 0 }
            ? (int)(Validation.MappedRequiredAttributes * 100.0 / Validation.TotalRequiredAttributes)
            : 100;
    public bool ReadyToPublish => IsMapped && Validation is { IsValid: true };
}
