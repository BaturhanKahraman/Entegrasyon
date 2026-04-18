using Entegrasyon.Entity.Dtos.Search;

namespace Entegrasyon.Business.Abstract;

public interface IGlobalSearchManager
{
    Task<SearchResponseDto> SearchAsync(
        string query,
        IReadOnlySet<string> userPermissions,
        IReadOnlyCollection<string>? sourceFilter = null,
        int perSourceLimit = 5,
        CancellationToken ct = default);
}
