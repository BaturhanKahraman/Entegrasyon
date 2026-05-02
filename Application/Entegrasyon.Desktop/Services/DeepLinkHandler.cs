using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Desktop.Printing.Transport;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Desktop.Services;

public sealed record DeepLinkInvocation(Guid BatchId, string Token);

public sealed record RemotePrintBatchItem(
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("printerLanguage")] string PrinterLanguage,
    [property: JsonPropertyName("zplContent")] string ZplContent,
    [property: JsonPropertyName("rawBytes")] byte[]? RawBytes,
    [property: JsonPropertyName("description")] string Description);

public sealed record RemotePrintBatchResponse(
    [property: JsonPropertyName("batchId")] Guid BatchId,
    [property: JsonPropertyName("totalItems")] int TotalItems,
    [property: JsonPropertyName("items")] List<RemotePrintBatchItem> Items);

public sealed record StatusUpdateDto(
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("printed")] bool Printed,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);

public sealed record StatusSubmitRequest(
    [property: JsonPropertyName("updates")] List<StatusUpdateDto> Updates);

public sealed class DeepLinkHandler(
    DeviceCredentialStore credentialStore,
    IPrinterTransport networkTransport,
    HttpClient httpClient,
    ILogger<DeepLinkHandler> logger)
{
    private const string Scheme = "entegrasyon-print://";

    public static DeepLinkInvocation? TryParse(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!url.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)) return null;

        var rest = url[Scheme.Length..];
        const string batchPrefix = "batch/";
        if (!rest.StartsWith(batchPrefix, StringComparison.OrdinalIgnoreCase)) return null;

        var afterBatch = rest[batchPrefix.Length..];
        var qIndex = afterBatch.IndexOf('?');
        if (qIndex < 0) return null;

        var idPart = afterBatch[..qIndex];
        if (!Guid.TryParse(idPart, out var batchId)) return null;

        var query = afterBatch[(qIndex + 1)..];
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "t")
                return new DeepLinkInvocation(batchId, Uri.UnescapeDataString(kv[1]));
        }
        return null;
    }

    public async Task HandleAsync(DeepLinkInvocation invocation, CancellationToken ct = default)
    {
        var credentials = credentialStore.TryLoad();
        if (credentials is null)
        {
            logger.LogError("Cihaz kayıtlı değil — credentials.json yok. Önce davet kodu ile kurulum yapın.");
            return;
        }

        logger.LogInformation("DeepLink alındı: batch={BatchId}", invocation.BatchId);

        var batch = await FetchBatchAsync(credentials, invocation, ct);
        if (batch is null) return;

        var statusUpdates = new List<StatusUpdateDto>();
        foreach (var item in batch.Items.OrderBy(i => i.Order))
        {
            var data = item.PrinterLanguage == "ESCPOS" && item.RawBytes is { Length: > 0 }
                ? item.RawBytes
                : System.Text.Encoding.UTF8.GetBytes(item.ZplContent ?? string.Empty);

            // Şimdilik network transport — printers.json'daki varsayılan yazıcıya gider.
            // İlerleyen sürümlerde printer adı item-spesifik atanabilir.
            try
            {
                var result = await networkTransport.SendRawAsync("Varsayilan-Yazici", data, ct);
                statusUpdates.Add(new StatusUpdateDto(item.Order, result.Success, result.Success ? null : result.Message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Yazdırma hatası: order={Order}", item.Order);
                statusUpdates.Add(new StatusUpdateDto(item.Order, false, ex.Message));
            }
        }

        await ReportStatusAsync(credentials, invocation, statusUpdates, ct);
    }

    private async Task<RemotePrintBatchResponse?> FetchBatchAsync(
        DeviceCredentials credentials, DeepLinkInvocation invocation, CancellationToken ct)
    {
        var url = $"{credentials.ServerBaseUrl.TrimEnd('/')}/api/print/batch/{invocation.BatchId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.DeviceApiKey);
        req.Headers.Add("X-Batch-Token", invocation.Token);

        try
        {
            var response = await httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Sunucudan batch alınamadı: {Status} {Body}", response.StatusCode, body);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<RemotePrintBatchResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch fetch network hatası");
            return null;
        }
    }

    private async Task ReportStatusAsync(
        DeviceCredentials credentials, DeepLinkInvocation invocation,
        List<StatusUpdateDto> updates, CancellationToken ct)
    {
        var url = $"{credentials.ServerBaseUrl.TrimEnd('/')}/api/print/batch/{invocation.BatchId}/status";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new StatusSubmitRequest(updates))
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.DeviceApiKey);
        req.Headers.Add("X-Batch-Token", invocation.Token);

        try
        {
            await httpClient.SendAsync(req, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Status raporu gönderilemedi (sessizce devam)");
        }
    }
}
