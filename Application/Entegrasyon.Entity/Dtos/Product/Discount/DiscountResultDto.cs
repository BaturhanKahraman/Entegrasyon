namespace Entegrasyon.Entity.Dtos.Product.Discount;

public sealed record DiscountResultDto(
    int VariantsUpdated,
    List<string> SyncTriggeredMarketplaces
);
