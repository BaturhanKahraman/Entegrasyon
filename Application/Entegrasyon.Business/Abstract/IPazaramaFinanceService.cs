using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaFinanceService
{
    /// <summary>
    /// Finans/muhasebe sorgusu yapar (tarih aralığı + opsiyonel orderId filtresi).
    /// POST /order/paymentAgreement
    /// </summary>
    Task<IDataResult<PazaramaFinanceData>> GetPaymentAgreementAsync(
        DateTimeOffset startDate, DateTimeOffset endDate, long? orderId = null);
}
