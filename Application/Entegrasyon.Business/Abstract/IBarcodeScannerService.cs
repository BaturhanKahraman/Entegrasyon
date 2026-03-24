using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBarcodeScannerService
{
    Task<BarcodeScannerConfig> GetConfigAsync(CancellationToken ct = default);
    Task<IResult> UpdateConfigAsync(BarcodeScannerConfig config, CancellationToken ct = default);
}
