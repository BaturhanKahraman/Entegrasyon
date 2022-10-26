namespace Shared.Entity;

public sealed class Pageable<T>
{
    public IEnumerable<T> Items { get; set; }
    public int CurrentPageIndex { get; set; }
    public int PagingItemCount { get; set; }
    public int TotalItemCount { get; set; }
    public int TotalPageCount => Convert.ToInt32(Math.Ceiling(TotalItemCount / (double)PagingItemCount));

    public Pageable()
    {
        
    }
    public Pageable(IEnumerable<T> items, int currentPage, int pagingItemCount, int totalItemCount)
    {
        Items = items;
        CurrentPageIndex = currentPage;
        PagingItemCount = pagingItemCount;
        TotalItemCount = totalItemCount;
    }
}
