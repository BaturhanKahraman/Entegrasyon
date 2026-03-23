using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama finans/muhasebe servisi (gerçek API çağrıları).
/// POST /order/paymentAgreement
/// </summary>
public sealed class PazaramaFinanceService(
    IPazaramaApiClient apiClient,
    ILogger<PazaramaFinanceService> logger) : IPazaramaFinanceService
{
    public async Task<IDataResult<PazaramaFinanceData>> GetPaymentAgreementAsync(
        DateTimeOffset startDate, DateTimeOffset endDate, long? orderId = null)
    {
        try
        {
            var request = new PazaramaFinanceRequest(
                StartDate: startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndDate: endDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                AllowanceDate: null,
                OrderId: orderId);

            logger.LogInformation("Pazarama GetPaymentAgreement başlatıldı. {Start} - {End}, OrderId: {OrderId}",
                startDate, endDate, orderId);

            var response = await apiClient.PostAsync("order/paymentAgreement", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetPaymentAgreement başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PazaramaFinanceData>(null, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaFinanceData>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Finans verisi alınamadı";
                logger.LogWarning("Pazarama GetPaymentAgreement yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<PazaramaFinanceData>(null, msg);
            }

            var data = parsed.Data ?? new PazaramaFinanceData(
                new List<PazaramaFinanceTransaction>(), 0m, 0m, 0m);

            logger.LogInformation("Pazarama GetPaymentAgreement başarılı. İşlem sayısı: {Count}",
                data.TransactionList?.Count ?? 0);
            return new SuccessDataResult<PazaramaFinanceData>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetPaymentAgreement exception. StartDate: {Start}, EndDate: {End}",
                startDate, endDate);
            return new ErrorDataResult<PazaramaFinanceData>(null, $"Hata: {ex.Message}");
        }
    }
}
