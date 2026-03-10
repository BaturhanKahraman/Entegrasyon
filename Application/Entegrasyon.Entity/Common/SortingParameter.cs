namespace Entegrasyon.Entity;

public sealed record SortingParameter
{
    public required string SortBy { get; init; }
    public OrderDirection OrderDirection { get; init; }
    public byte Order { get; init; }
}

public enum OrderDirection
{
    Ascending = 1,
    Descending
}
