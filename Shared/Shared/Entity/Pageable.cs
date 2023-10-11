namespace Shared.Entity;

public sealed class Pageable<T>
{
    public IReadOnlyList<T> Items { get; set; }
    public int CurrentPageIndex { get; set; }
    public int PagingItemCount { get; set; }
    public int TotalItemCount { get; set; }
    public int TotalPageCount => Convert.ToInt32(Math.Ceiling(TotalItemCount / (double)PagingItemCount));
    
    public Pageable(IReadOnlyList<T> items, int currentPage, int pagingItemCount, int totalItemCount)
    {
        Items = items;
        CurrentPageIndex = currentPage;
        PagingItemCount = pagingItemCount;
        TotalItemCount = totalItemCount;
    }
    public Pageable()
    {
        // Default constructor without arguments
    }
}
