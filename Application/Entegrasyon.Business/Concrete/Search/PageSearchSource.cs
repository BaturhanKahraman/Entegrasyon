using Entegrasyon.Business.Abstract.Search;
using Entegrasyon.Entity.Dtos.Search;

namespace Entegrasyon.Business.Concrete.Search;

public sealed class PageSearchSource : ISearchSource
{
    public string SourceKey => "pages";
    public string Label => "Sayfalar";
    public string Icon => "ti-layout";
    public string? RequiredPermission => null;

    public Task<IReadOnlyList<SearchHitDto>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<SearchHitDto>>([]);

        var q = query.Trim().ToLowerInvariant();

        var hits = AppPages.All
            .Select(p => new { Page = p, Score = ScorePage(p, q) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => new SearchHitDto(
                Title: x.Page.Title,
                Subtitle: x.Page.Url,
                Url: x.Page.Url,
                Score: x.Score,
                Badge: null))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchHitDto>>(hits);
    }

    private static double ScorePage(AppPage page, string q)
    {
        var title = page.Title.ToLowerInvariant();
        if (title == q) return 1.0;
        if (title.StartsWith(q)) return 0.9;
        if (title.Contains(q)) return 0.7;

        foreach (var kw in page.Keywords)
        {
            var k = kw.ToLowerInvariant();
            if (k == q) return 0.85;
            if (k.StartsWith(q)) return 0.75;
            if (k.Contains(q)) return 0.55;
        }

        return 0.0;
    }
}
