using Entegrasyon.Entity.Templates;

namespace Entegrasyon.Entity.Requests;

public record MatchedEntityPackagePaginatedRequest() : PaginatedRequest()
{
    public MatchedEntityType? EntityType { get; init; }
}
