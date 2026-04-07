using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Trendyol e-Faturam entegrasyonu — e-arsiv ve e-fatura islemleri.
/// Mukellef sorgulama, fatura olusturma, durum takibi, iptal, PDF indirme.
/// </summary>
public interface ITrendyolEFaturaService
{
    /// <summary>
    /// VKN/TCKN ile mukellef sorgula.
    /// true = e-fatura mukellefiyse (aliasType == INVOICE), false = degil (e-arsiv kullanilacak).
    /// </summary>
    Task<IDataResult<bool>> CheckTaxPayerAsync(string taxId, CancellationToken ct = default);

    /// <summary>
    /// Sipariş icin e-fatura veya e-arsiv olustur.
    /// Alicinin mukellef durumuna gore otomatik karar verir.
    /// </summary>
    Task<IDataResult<EFaturaRecord>> CreateInvoiceForOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Mevcut fatura kaydinin durumunu Trendyol API'den sorgular ve gunceller.
    /// </summary>
    Task<IDataResult<EFaturaRecord>> CheckInvoiceStatusAsync(Guid invoiceRecordId, CancellationToken ct = default);

    /// <summary>
    /// e-Arsiv faturayi iptal eder. (Sadece e-arsiv icin gecerli.)
    /// </summary>
    Task<IResult> CancelInvoiceAsync(Guid invoiceRecordId, CancellationToken ct = default);

    /// <summary>
    /// Onaylanmis fatura icin kalici PDF indirme URL'i alir.
    /// </summary>
    Task<IDataResult<string>> GetInvoicePdfUrlAsync(Guid invoiceRecordId, CancellationToken ct = default);

    /// <summary>
    /// Kalan kontor/kredi sorgular.
    /// </summary>
    Task<IDataResult<int>> GetRemainingCreditsAsync(CancellationToken ct = default);
}
