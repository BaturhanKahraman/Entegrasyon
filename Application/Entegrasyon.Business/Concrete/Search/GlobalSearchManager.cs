using System.Diagnostics;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.Entity.Dtos.Search;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class GlobalSearchManager(IEnumerable<ISearchSource> sources) : IGlobalSearchManager
{
    public async Task<SearchResponseDto> SearchAsync(
        string query,
        IReadOnlySet<string> userPermissions,
        IReadOnlyCollection<string>? sourceFilter = null,
        int perSourceLimit = 5,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(query))
            return new SearchResponseDto([], 0, 0);

        var selected = sources.Where(s =>
        {
            if (sourceFilter is { Count: > 0 } && !sourceFilter.Contains(s.SourceKey))
                return false;
            if (s.RequiredPermission is { } perm && !userPermissions.Contains(perm))
                return false;
            return true;
        }).ToList();

        var tasks = selected.Select(async s =>
        {
            try
            {
                var hits = await s.SearchAsync(query, perSourceLimit, ct);
                return new SearchGroupDto(s.SourceKey, s.Label, s.Icon, hits);
            }
            catch
            {
                // Tek bir kaynağın hatası diğerlerini engellemesin
                return new SearchGroupDto(s.SourceKey, s.Label, s.Icon, []);
            }
        });

        var groups = await Task.WhenAll(tasks);

        var nonEmpty = groups.Where(g => g.Hits.Count > 0).ToList();
        var total = nonEmpty.Sum(g => g.Hits.Count);

        sw.Stop();
        return new SearchResponseDto(nonEmpty, total, sw.ElapsedMilliseconds);
    }
}
