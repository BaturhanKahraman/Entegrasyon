namespace Entegrasyon.Entity.Dtos.Reports;

/// <summary>
/// Müşteri raporu "İndirim Kodu Gönder" dropdown'unda gösterilen aktif/gönderilebilir kupon.
/// View select option'ında <see cref="Id"/> (value), <see cref="Code"/> ve <see cref="Description"/>
/// (insan-okunur indirim tutarı) kullanır.
/// </summary>
public sealed record SendableCouponDto(int Id, string Code, string Description);
