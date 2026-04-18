namespace Entegrasyon.Entity.Dtos.Search;

public sealed record SearchGroupDto(
    string SourceKey,
    string Label,
    string Icon,
    IReadOnlyList<SearchHitDto> Hits);
