using Shared.DTO;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SalePageableDto(
    int? CustomerId,
    DateTimeOffset? DateBetweenStart,
    DateTimeOffset? DateBetweenEnd,
    Guid SalePersonId,
    string FullTextSearchKey,int PageIndex = 0,int PageSize = 50
    ) :SearchablePageDto(FullTextSearchKey,PageIndex,PageSize);