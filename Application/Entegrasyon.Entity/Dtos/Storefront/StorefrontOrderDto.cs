namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontOrderListDto(
    Guid Id, string OrderNumber, DateTimeOffset OrderDate,
    decimal Total, int ItemCount, string Status, string? TrackingNumber);

public record StorefrontOrderDetailDto(
    Guid Id, string OrderNumber, DateTimeOffset OrderDate,
    decimal SubTotal, decimal ShippingCost, decimal DiscountAmount, decimal Total,
    string Status, string PaymentStatus,
    string? TrackingNumber, string? TrackingLink, string? CargoProvider,
    string? ShippingAddress, string? CustomerName, string? CustomerEmail,
    string? OrderNote, List<StorefrontOrderItemDto> Items);

public record StorefrontOrderItemDto(
    string ProductTitle, string? ImageUrl, string? VariantInfo,
    int Quantity, decimal UnitPrice, decimal LineTotal);
