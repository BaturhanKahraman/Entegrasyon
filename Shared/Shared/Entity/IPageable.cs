namespace Shared.Entity;

public sealed class Pageable<T>
{
    public List<T> Items { get; set; }
    public int CurrentPage { get; set; }
    public int PagingItemCount { get; set; }
    public int TotalItemCount { get; set; }
    public int TotalPageCount { get; set; }
}
