using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Aras Kargo mock servisi. Test ve development ortami icin.
/// Gercek API cagirisi yapmaz, basarili sonuc doner.
/// </summary>
public sealed class MockArasKargoService(
    ILogger<MockArasKargoService> logger) : IArasKargoService
{
    public Task<IDataResult<ArasKargoOrderResult>> CreateShipmentAsync(
        ArasKargoShipmentRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Aras Kargo gonderi olusturuldu: {IntegrationCode}", request.IntegrationCode);

        var result = new ArasKargoOrderResult(
            ResultCode: "0",
            ResultMessage: "Basarili (Mock)",
            BarcodeNumber: $"MOCK-BRK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}");

        return Task.FromResult<IDataResult<ArasKargoOrderResult>>(
            new SuccessDataResult<ArasKargoOrderResult>(result));
    }

    public Task<IResult> CancelShipmentAsync(
        string integrationCode, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Aras Kargo gonderi iptal edildi: {IntegrationCode}", integrationCode);

        return Task.FromResult<IResult>(new SuccessResult("Gonderi basariyla iptal edildi. (Mock)"));
    }

    public Task<IDataResult<ArasKargoTrackingResult>> TrackShipmentAsync(
        string integrationCode, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Aras Kargo gonderi takip: {IntegrationCode}", integrationCode);

        var result = new ArasKargoTrackingResult(
            IntegrationCode: integrationCode,
            Status: "Teslim Edildi",
            ReceiverName: "Mock Alici",
            DeliveryDate: DateTime.UtcNow.ToString("dd-MM-yyyy"),
            BarcodeNumber: "MOCK-BRK-12345678");

        return Task.FromResult<IDataResult<ArasKargoTrackingResult>>(
            new SuccessDataResult<ArasKargoTrackingResult>(result));
    }

    public Task<IDataResult<List<ArasKargoMovement>>> GetShipmentMovementsAsync(
        string integrationCode, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Aras Kargo hareket sorgulama: {IntegrationCode}", integrationCode);

        var movements = new List<ArasKargoMovement>
        {
            new("2026-03-23", "Kabul", "Istanbul", "Kargo kabul edildi (Mock)"),
            new("2026-03-23", "Aktarma", "Ankara", "Transfer merkezinde (Mock)"),
            new("2026-03-23", "Teslim Edildi", "Ankara", "Aliciya teslim edildi (Mock)")
        };

        return Task.FromResult<IDataResult<List<ArasKargoMovement>>>(
            new SuccessDataResult<List<ArasKargoMovement>>(movements));
    }

    public Task<IDataResult<List<ArasKargoShipmentSummary>>> GetShipmentsByDateRangeAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Aras Kargo tarih araligi sorgulama: {Start} - {End}",
            startDate.ToString("dd-MM-yyyy"), endDate.ToString("dd-MM-yyyy"));

        var shipments = new List<ArasKargoShipmentSummary>
        {
            new("MOCK-INT-001", "Teslim Edildi", "Mock Alici 1", startDate.ToString("dd-MM-yyyy")),
            new("MOCK-INT-002", "Dagitimda", "Mock Alici 2", endDate.ToString("dd-MM-yyyy"))
        };

        return Task.FromResult<IDataResult<List<ArasKargoShipmentSummary>>>(
            new SuccessDataResult<List<ArasKargoShipmentSummary>>(shipments));
    }

    public Task<IDataResult<List<ArasKargoShipmentSummary>>> GetUndeliveredShipmentsAsync(
        CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Aras Kargo teslim edilmemis kargolar sorgulanidi");

        var shipments = new List<ArasKargoShipmentSummary>
        {
            new("MOCK-INT-003", "Dagitimda", "Mock Alici 3", DateTime.UtcNow.ToString("dd-MM-yyyy"))
        };

        return Task.FromResult<IDataResult<List<ArasKargoShipmentSummary>>>(
            new SuccessDataResult<List<ArasKargoShipmentSummary>>(shipments));
    }
}
