using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti fatura gönderim servisi.
/// Fatura belgelerini /Branch/SendInvoiceMail endpoint'ine iletir.
/// ÖNEMLI: Bu endpoint /api/v1 prefix'i KULLANMAZ — SendRawAsync çağrılmalıdır.
/// </summary>
public sealed class CiceksepetiInvoiceService(
    ICiceksepetiApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<CiceksepetiInvoiceService> logger) : ICiceksepetiInvoiceService
{
    // NOTE: This path does NOT use /api/v1/ prefix — use SendRawAsync
    private const string InvoiceEndpoint = "Branch/SendInvoiceMail";

    public async Task<IResult> SendInvoiceAsync(
        CiceksepetiInvoiceRequest request,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation(
                "CiceksepetiInvoiceService: Fatura gönderiliyor. {Count} kalem.",
                request.Items.Count);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // CRITICAL: Use SendRawAsync — this endpoint path bypasses /api/v1/
            var response = await apiClient.SendRawAsync(InvoiceEndpoint, HttpMethod.Post, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"Fatura gönderilemedi. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiInvoiceService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Invoice, LogAction.Update, new { request }, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("CiceksepetiInvoiceService: Fatura başarıyla gönderildi.");
            return new SuccessResult("Fatura başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiInvoiceService: Fatura gönderilemedi.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti fatura gönderimi başarısız: {ex.Message}",
                LogType.Invoice, LogAction.Update, new { request }, ct);
            return new ErrorResult(ex.Message);
        }
    }
}
