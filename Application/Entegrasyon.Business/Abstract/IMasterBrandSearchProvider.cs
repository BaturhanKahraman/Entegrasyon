using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Master DB'den (AdminPanel) marketplace markalarını arar.
/// Business katmanı AdminPanel projesine bağımlı olmadan bu arayüz üzerinden erişir.
/// </summary>
public interface IMasterBrandSearchProvider
{
    Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(
        int marketPlaceId, string query, CancellationToken ct = default);
}
