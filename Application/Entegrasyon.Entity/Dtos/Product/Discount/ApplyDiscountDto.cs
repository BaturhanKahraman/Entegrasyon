namespace Entegrasyon.Entity.Dtos.Product.Discount;

public sealed record ApplyDiscountDto(
    Guid ProductId,
    decimal DiscountPercentage,
    List<int> TargetMarketPlaceIds
);
