namespace Entegrasyon.Entity.Dtos.Storefront;

public record CartDto(
    Guid Id, List<CartItemDto> Items, string? CouponCode,
    decimal SubTotal, decimal ShippingCost, decimal GrandTotal, int ItemCount);

public record CartItemDto(
    Guid ProductVariantId, string ProductTitle, string? ImageUrl,
    string? VariantInfo, int Quantity, decimal UnitPrice,
    decimal LineTotal, int AvailableStock);
