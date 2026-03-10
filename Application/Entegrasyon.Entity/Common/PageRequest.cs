namespace Entegrasyon.Entity;

public abstract record PaginatedRequest(
    int PageIndex = 0,
    int PageSize = 10,
    string? SearchTerm = null
)
{
    public List<string> OrderBy { get; init; } = new();
}
