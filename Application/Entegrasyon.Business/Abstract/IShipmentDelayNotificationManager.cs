using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Gecikmiş gönderilerin müşterilerine toplu bilgilendirme — MUTASYON (tam pipeline + çift log).
/// Order.CustomerEmail tanımlı olanlara e-posta gönderilir; tanımsızlar atlanır.
/// </summary>
public interface IShipmentDelayNotificationManager
{
    Task<IResult> NotifyDelayedCustomersAsync(NotifyDelayedShipmentsDto dto, Guid? userId, CancellationToken ct = default);
}
