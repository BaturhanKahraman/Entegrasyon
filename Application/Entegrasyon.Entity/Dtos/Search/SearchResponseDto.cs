namespace Entegrasyon.Entity.Dtos.Search;

public sealed record SearchResponseDto(
    IReadOnlyList<SearchGroupDto> Groups,
    int TotalHits,
    long ElapsedMs);
