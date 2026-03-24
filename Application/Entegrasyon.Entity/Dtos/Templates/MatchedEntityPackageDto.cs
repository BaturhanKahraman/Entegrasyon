using Entegrasyon.Entity.Templates;

namespace Entegrasyon.Entity.Dtos.Templates;

/// <summary>
/// Paket listesi görünümü için DTO.
/// </summary>
public record MatchedEntityPackageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public MatchedEntityType EntityType { get; init; }
    public int Version { get; init; }
    public int MarketplaceCount { get; init; }
    public int EntityCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
