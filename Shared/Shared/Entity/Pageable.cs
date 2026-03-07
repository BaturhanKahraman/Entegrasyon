namespace Shared.Entity;

public sealed record Pageable<T>(
    IReadOnlyList<T> Items,
    int CurrentPageIndex,
    int PageSize,
    int TotalItemCount)
{
    public int TotalPageCount => TotalItemCount == 0 ? 0 : (int)Math.Ceiling(TotalItemCount / (double)PageSize);
    public bool HasItem => Items.Count > 0;
}
