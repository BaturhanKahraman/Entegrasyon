namespace Entegrasyon.Entity;

public sealed record QueryParameter
{
    /// <summary>
    /// Page number for query parameters
    /// </summary>
    public int Page { get; init; }
    /// <summary>
    /// Page index for query parameter (if necessary)
    /// </summary>
    public int PageIndex => Page == 0 ? 0 : Page - 1;
    public uint ItemCount { get; init; }

    public IEnumerable<FilterParameter> Filters { get; init; } = new List<FilterParameter>();
    public IEnumerable<SortingParameter> SortingParameters { get; init; } = new List<SortingParameter>();
}
