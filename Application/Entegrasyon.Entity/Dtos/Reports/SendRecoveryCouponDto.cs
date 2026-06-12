namespace Entegrasyon.Entity.Dtos.Reports;

/// <summary>
/// Müşteri raporundan seçili müşterilere toplu indirim kodu (dormant geri kazanım) gönderme isteği.
/// <see cref="VoucherId"/> mevcut bir DiscountVoucher'a işaret eder; <see cref="CustomerIds"/> seçili
/// müşterilerdir. <see cref="Segment"/> yalnızca loglama/bağlam içindir (hangi segmentten gönderildi).
/// </summary>
public sealed record SendRecoveryCouponDto(int VoucherId, IReadOnlyList<int> CustomerIds, string? Segment);
