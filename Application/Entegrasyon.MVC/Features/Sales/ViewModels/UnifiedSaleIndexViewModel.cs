using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Sale;

namespace Entegrasyon.MVC.Features.Sales.ViewModels;

public sealed class UnifiedSaleIndexViewModel
{
    public Pageable<UnifiedSaleListItemDto> Page { get; init; } =
        new Pageable<UnifiedSaleListItemDto>([], 0, 25, 0);

    public UnifiedSaleSummaryDto Summary { get; init; } =
        new UnifiedSaleSummaryDto(0m, 0, 0m, 0d);

    public List<UnifiedSaleSourceCountDto> SourceCounts { get; init; } = [];

    public UnifiedSaleFilterDto Filter { get; init; } =
        new UnifiedSaleFilterDto(null, null, null, null, null, null);
}
