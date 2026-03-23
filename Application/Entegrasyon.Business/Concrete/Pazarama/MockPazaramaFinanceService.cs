using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama finans servisi — mock implementasyon (test/geliştirme ortamı).
/// Gerçek API çağrısı yapmaz; boş transaction listesiyle başarı döner.
/// </summary>
public sealed class MockPazaramaFinanceService(
    ILogger<MockPazaramaFinanceService> logger) : IPazaramaFinanceService
{
    public Task<IDataResult<PazaramaFinanceData>> GetPaymentAgreementAsync(
        DateTimeOffset startDate, DateTimeOffset endDate, long? orderId = null)
    {
        logger.LogInformation("MockPazarama: GetPaymentAgreement {Start} - {End}, OrderId={OrderId}",
            startDate, endDate, orderId);

        var data = new PazaramaFinanceData(
            TransactionList: new List<PazaramaFinanceTransaction>(),
            TotalAmount: 0m,
            TotalCommission: 0m,
            TotalAllowance: 0m);

        return Task.FromResult<IDataResult<PazaramaFinanceData>>(
            new SuccessDataResult<PazaramaFinanceData>(data));
    }
}
