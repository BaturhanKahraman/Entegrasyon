namespace Shared.Entity;

public sealed class Pageable<T>
{
    public IEnumerable<T> Items { get; set; }
    public int CurrentPage { get; set; }
    public int PagingItemCount { get; set; }
    public int TotalItemCount { get; set; }
    public int TotalPageCount { get; set; }

    public Pageable()
    {
        
    }
    public Pageable(IEnumerable<T> items, int currentPage, int pagingItemCount, int totalItemCount, int totalPageCount)
    {
        Items = items;
        CurrentPage = currentPage;
        PagingItemCount = pagingItemCount;
        TotalItemCount = totalItemCount;
        TotalPageCount = totalPageCount;
    }
}
