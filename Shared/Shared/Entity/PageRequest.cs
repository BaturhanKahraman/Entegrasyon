namespace Shared.Entity;

public sealed record PageRequest<T> where T: class
{
    public int CurrentPageIndex => CurrentPage - 1;
    public int CurrentPage { get; set; }
    public int PageCount => ItemCount / ShowedItemSize;
    public int ItemCount { get; set; }
    public int ShowedItemSize { get; set; } = 50;


}