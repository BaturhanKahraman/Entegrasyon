using Entegrasyon.Entity.Dtos.Search;

namespace Entegrasyon.Business.Abstract.Search;

/// <summary>
/// Ctrl+K command palette ve global aramada her arama kaynağının (Product, Customer,
/// Category, Brand, Page, ...) uyguladığı kontrat. Her kaynak bağımsız bir DbContext
/// kullanır — Task.WhenAll ile güvenli paralel çağrı için.
/// </summary>
public interface ISearchSource
{
    string SourceKey { get; }
    string Label { get; }
    string Icon { get; }

    /// <summary>Bu kaynağa erişim için gereken permission anahtarı. null ise herkes arayabilir.</summary>
    string? RequiredPermission { get; }

    Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default);
}
