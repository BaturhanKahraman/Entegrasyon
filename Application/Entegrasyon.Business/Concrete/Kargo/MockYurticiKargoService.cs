using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.Kargo;

/// <summary>
/// Development ve test ortami icin Yurtici Kargo mock servisi.
/// Gercek API cagrisi yapmaz, sabit basarili sonuclar doner.
/// </summary>
public sealed class MockYurticiKargoService : IYurticiKargoService
{
    public Task<IDataResult<YurticiCreateShipmentResponse>> CreateShipmentAsync(
        YurticiCreateShipmentRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Task.FromResult<IDataResult<YurticiCreateShipmentResponse>>(
                new ErrorDataResult<YurticiCreateShipmentResponse>(null!, "Kargo olusturma istegi bos olamaz."));

        if (string.IsNullOrWhiteSpace(request.CargoKey))
            return Task.FromResult<IDataResult<YurticiCreateShipmentResponse>>(
                new ErrorDataResult<YurticiCreateShipmentResponse>(null!, "CargoKey bos olamaz."));

        var response = new YurticiCreateShipmentResponse("1", "Gonderi basariyla olusturuldu.", $"MOCK-JOB-{request.CargoKey}");
        return Task.FromResult<IDataResult<YurticiCreateShipmentResponse>>(
            new SuccessDataResult<YurticiCreateShipmentResponse>(response));
    }

    public Task<IDataResult<List<YurticiShipmentInfo>>> QueryShipmentAsync(
        YurticiQueryShipmentRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return Task.FromResult<IDataResult<List<YurticiShipmentInfo>>>(
                new ErrorDataResult<List<YurticiShipmentInfo>>(null!, "Kargo sorgulama istegi bos olamaz."));

        if (request.Keys is null || request.Keys.Length == 0)
            return Task.FromResult<IDataResult<List<YurticiShipmentInfo>>>(
                new ErrorDataResult<List<YurticiShipmentInfo>>(null!, "Sorgu anahtarlari bos olamaz."));

        var shipments = request.Keys.Select(key =>
            new YurticiShipmentInfo(
                CargoKey: key,
                InvoiceKey: $"INV-{key}",
                OperationCode: 4,
                OperationMessage: "Teslim edildi",
                DeliveryDate: DateTime.UtcNow.AddDays(-1),
                DeliveredTo: "Mock Alici",
                UnitCount: 1)).ToList();

        return Task.FromResult<IDataResult<List<YurticiShipmentInfo>>>(
            new SuccessDataResult<List<YurticiShipmentInfo>>(shipments));
    }

    public Task<IResult> CancelShipmentAsync(
        string cargoKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cargoKey))
            return Task.FromResult<IResult>(new ErrorResult("CargoKey bos olamaz."));

        return Task.FromResult<IResult>(new SuccessResult("Kargo basariyla iptal edildi."));
    }
}
