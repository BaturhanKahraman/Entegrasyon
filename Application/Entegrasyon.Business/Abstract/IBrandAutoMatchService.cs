using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBrandAutoMatchService
{
    /// <summary>
    /// Secilen marketplace icin eslenmemis markalari string matching + Ollama ile otomatik eslestirir.
    /// confidence >= 0.8 olan Eşleşmeleri otomatik kaydeder, Düşük guvenli olanlar onay icin doner.
    /// </summary>
    Task<IDataResult<BrandAutoMatchResultDto>> AutoMatchAsync(int marketPlaceId, CancellationToken ct = default);

    /// <summary>
    /// Ollama servisinin ayakta olup olmadigini kontrol eder.
    /// </summary>
    Task<bool> IsOllamaAvailableAsync(CancellationToken ct = default);
}
