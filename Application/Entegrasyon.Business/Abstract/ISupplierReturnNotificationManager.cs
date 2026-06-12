using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Yüksek iadeli ürünün tedarikçisine (markasına) bildirim — MUTASYON (tam pipeline + çift log).
/// Brand.SupplierEmail tanımlıysa e-posta gönderilir; değilse tetikleyen kullanıcıya in-app bildirim düşer.
/// </summary>
public interface ISupplierReturnNotificationManager
{
    Task<IResult> NotifyHighReturnAsync(NotifySupplierHighReturnDto dto, Guid? userId, CancellationToken ct = default);
}
