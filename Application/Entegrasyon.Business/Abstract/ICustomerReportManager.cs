using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Müşteri Raporu (RFM segmentasyon + cohort retention + dormant geri kazanım kuponu).
/// Read yolları salt-okuma; <see cref="SendRecoveryCouponAsync"/> mutasyon (tam pipeline + çift log).
/// </summary>
public interface ICustomerReportManager
{
    /// <summary>
    /// RFM-zenginleştirilmiş müşteri listesi (sayfalı). Her müşteriye LastPurchaseDate (recency),
    /// TotalSpend (monetary) ve RfmSegment ("VIP"|"Risk"|"Yeni"|"Dormant"|"Standart") doldurulur.
    /// VIP eşiği (üst %20 harcama persentili) server-side hesaplanır (full-load yok).
    /// <paramref name="segment"/>: ""|null=tümü, "vip"|"risk"|"yeni"|"dormant" → sonuç o segmente filtrelenir.
    /// </summary>
    Task<IDataResult<Pageable<CustomerDetailDto>>> GetRfmPageableAsync(
        string? segment, int pageIndex = 0, int itemCount = 50, CancellationToken ct = default);

    /// <summary>
    /// Aylık cohort retention matrisi. Müşteriler ilk-alışveriş ayına göre gruplanır; her kohort için
    /// sonraki aylarda tekrar alışveriş oranı (0-100) döner. En yeni kohort başta.
    /// </summary>
    Task<IReadOnlyList<CustomerCohortRowDto>> GetCohortRetentionAsync(int maxMonths = 12, CancellationToken ct = default);

    /// <summary>
    /// İndirim kodu gönderme dropdown'u için aktif (silinmemiş, süresi geçmemiş) kupon listesi.
    /// </summary>
    Task<IReadOnlyList<SendableCouponDto>> GetSendableCouponsAsync(CancellationToken ct = default);

    /// <summary>
    /// MUTASYON — seçili müşterilere kuponu (dormant geri kazanım) atar: voucher'ın CustomerId'sini
    /// hedef müşteriye bağlayarak müşteri başına bir kupon kopyası oluşturur. Tam pipeline:
    /// FluentValidation → iş kuralları (voucher var/aktif mi, müşteri var mı) → execution + çift log.
    /// </summary>
    Task<IResult> SendRecoveryCouponAsync(SendRecoveryCouponDto dto, Guid? userId, CancellationToken ct = default);
}
