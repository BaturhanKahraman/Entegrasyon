using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol.EFatura;

/// <summary>
/// Mock e-Faturam API client — gelistirme/test ortami icin.
/// </summary>
public sealed class MockTrendyolEFaturaApiClient(
    ILogger<MockTrendyolEFaturaApiClient> logger) : ITrendyolEFaturaApiClient
{
    public Task<HttpResponseMessage> GetAsync(string relativeUrl, CancellationToken ct = default)
    {
        logger.LogInformation("Mock e-Fatura GET: {Url}", relativeUrl);

        // Mukellef sorgulama mock
        if (relativeUrl.Contains("taxpayers/"))
        {
            var taxPayerJson = JsonSerializer.Serialize(new[]
            {
                new { taxId = "1234567890", alias = "urn:mail:test@test.com", title = "Test Firma", aliasType = "INVOICE" }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(taxPayerJson, System.Text.Encoding.UTF8, "application/json")
            });
        }

        // Status sorgulama mock — onaylandi
        if (relativeUrl.Contains("/status/"))
        {
            var statusJson = JsonSerializer.Serialize(new { status = 205, gibStatus = "REPORTED", invoiceUuid = "mock-uuid" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(statusJson, System.Text.Encoding.UTF8, "application/json")
            });
        }

        // Kalan kontor mock
        if (relativeUrl.Contains("credits/remaining"))
        {
            var creditJson = JsonSerializer.Serialize(new { taxId = "1234567890", remainingCredit = 100, remainingCreditStatus = "SUFFICIENT" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(creditJson, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body, CancellationToken ct = default)
    {
        logger.LogInformation("Mock e-Fatura POST: {Url}", relativeUrl);

        // Fatura olusturma mock
        if (relativeUrl.Contains("earchive") || relativeUrl.Contains("outgoing-einvoice"))
        {
            var createJson = JsonSerializer.Serialize(new
            {
                id = 12345L,
                invoiceUuid = Guid.NewGuid().ToString(),
                invoiceId = "MCK2025000000001",
                status = 10,
                scenario = "EARSIVFATURA",
                invoiceTypeCode = "SATIS",
                payableAmount = 11455L,
                taxAmount = 1755L
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(createJson, System.Text.Encoding.UTF8, "application/json")
            });
        }

        // PDF indirme mock
        if (relativeUrl.Contains("download/permanent-url"))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"https://mock-pdf.example.com/invoice.pdf\"",
                    System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
