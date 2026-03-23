using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Surat Kargo mock servisi. Development ve test ortamlarinda kullanilir.
/// Gercek API'ye baglanti kurmadan sahte veriler doner.
/// </summary>
public sealed class MockSuratKargoService(
    ILogger<MockSuratKargoService> logger) : ISuratKargoService
{
    private static int _counter;

    public Task<IDataResult<SuratKargoShipmentResult>> CreateShipmentAsync(
        SuratKargoShipmentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiverName))
            return Task.FromResult<IDataResult<SuratKargoShipmentResult>>(
                new ErrorDataResult<SuratKargoShipmentResult>(null!, "Alıcı adı boş olamaz."));

        if (string.IsNullOrWhiteSpace(request.ReceiverPhone))
            return Task.FromResult<IDataResult<SuratKargoShipmentResult>>(
                new ErrorDataResult<SuratKargoShipmentResult>(null!, "Alıcı telefon numarası boş olamaz."));

        if (string.IsNullOrWhiteSpace(request.ReceiverAddress))
            return Task.FromResult<IDataResult<SuratKargoShipmentResult>>(
                new ErrorDataResult<SuratKargoShipmentResult>(null!, "Alıcı adresi boş olamaz."));

        if (request.PieceCount <= 0)
            return Task.FromResult<IDataResult<SuratKargoShipmentResult>>(
                new ErrorDataResult<SuratKargoShipmentResult>(null!, "Parça sayısı sıfırdan büyük olmalıdır."));

        var counter = Interlocked.Increment(ref _counter);
        var trackingNumber = $"MOCK-SK-{counter:D8}";
        var barcodeNo = $"MOCK-BC-{counter:D8}";

        logger.LogInformation("[MOCK] SuratKargo shipment created: tracking={Tracking}, receiver={Receiver}",
            trackingNumber, request.ReceiverName);

        var result = new SuratKargoShipmentResult(trackingNumber, barcodeNo, 0, "Başarılı (mock)");
        return Task.FromResult<IDataResult<SuratKargoShipmentResult>>(
            new SuccessDataResult<SuratKargoShipmentResult>(result));
    }

    public Task<IDataResult<SuratKargoTrackingResult>> QueryShipmentAsync(
        string trackingNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Task.FromResult<IDataResult<SuratKargoTrackingResult>>(
                new ErrorDataResult<SuratKargoTrackingResult>(null!, "Takip numarası boş olamaz."));

        logger.LogInformation("[MOCK] SuratKargo query: tracking={Tracking}", trackingNumber);

        var movements = new List<SuratKargoMovement>
        {
            new(DateTime.UtcNow.AddHours(-24), "Istanbul - Dagitim Merkezi", "Kargoya verildi"),
            new(DateTime.UtcNow.AddHours(-12), "Istanbul - Transfer Merkezi", "Transfer merkezine ulasti"),
            new(DateTime.UtcNow.AddHours(-2), "Ankara - Dagitim Merkezi", "Dagitima cikti")
        };

        var result = new SuratKargoTrackingResult(trackingNumber, "Dagitimda", null, movements);
        return Task.FromResult<IDataResult<SuratKargoTrackingResult>>(
            new SuccessDataResult<SuratKargoTrackingResult>(result));
    }

    public Task<IResult> CancelShipmentAsync(
        string trackingNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Task.FromResult<IResult>(new ErrorResult("Takip numarası boş olamaz."));

        logger.LogInformation("[MOCK] SuratKargo shipment cancelled: {Tracking}", trackingNumber);
        return Task.FromResult<IResult>(new SuccessResult("Gönderi başarıyla iptal edildi (mock)."));
    }

    public Task<IDataResult<string>> GetBarcodeAsync(
        string referenceNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(referenceNo))
            return Task.FromResult<IDataResult<string>>(
                new ErrorDataResult<string>(null!, "Referans numarası boş olamaz."));

        logger.LogInformation("[MOCK] SuratKargo barcode for ref={Ref}", referenceNo);

        // Mock base64 barkod verisi
        var mockBarcode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"BARCODE-{referenceNo}"));
        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>(mockBarcode));
    }

    public Task<IDataResult<string>> GetShipmentLabelAsync(
        string trackingNumber, string? labelFormat = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Task.FromResult<IDataResult<string>>(
                new ErrorDataResult<string>(null!, "Takip numarası boş olamaz."));

        logger.LogInformation("[MOCK] SuratKargo label for tracking={Tracking}, format={Format}",
            trackingNumber, labelFormat ?? "default");

        // Mock base64 etiket verisi
        var mockLabel = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"LABEL-{trackingNumber}-{labelFormat ?? "PDF"}"));
        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>(mockLabel));
    }
}
