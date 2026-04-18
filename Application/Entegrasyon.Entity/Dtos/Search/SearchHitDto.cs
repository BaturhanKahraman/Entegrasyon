namespace Entegrasyon.Entity.Dtos.Search;

public sealed record SearchHitDto(
    string Title,
    string? Subtitle,
    string Url,
    double Score,
    string? Badge = null);
