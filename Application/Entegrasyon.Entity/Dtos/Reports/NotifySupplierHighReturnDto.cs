namespace Entegrasyon.Entity.Dtos.Reports;

/// <summary>
/// Yüksek iadeli bir ürünün tedarikçisine (markasına) bildirim göndermek için istek.
/// İade raporundaki "Ürün Bazlı İade Oranı" satırından tetiklenir.
/// </summary>
public record NotifySupplierHighReturnDto(
    Guid ProductVariantId,
    double ReturnRatePercent);
