using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SalePageableDto(
    int? CustomerId,
    DateTimeOffset? DateBetweenStart,
    DateTimeOffset? DateBetweenEnd,
    Guid SalePersonId,
    SaleSource? SaleSource,
    SaleStatus? SaleStatus,
    string FullTextSearchKey,
    int PageIndex = 0,
    int PageSize = 50
) : SearchablePageDto(FullTextSearchKey, PageIndex, PageSize);
