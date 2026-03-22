namespace Entegrasyon.Entity.Dtos.N11;

public record N11OrderDto
{
    public long Id { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public string? PaymentType { get; init; }
    public DateTimeOffset CreateDate { get; init; }
    public string? CitizenshipId { get; init; }
    public N11BuyerDto? Buyer { get; init; }
    public N11AddressDto? BillingAddress { get; init; }
    public N11AddressDto? ShippingAddress { get; init; }
    public List<N11OrderItemDto> OrderItems { get; init; } = new();
}

public record N11OrderItemDto
{
    public long Id { get; init; }
    public long ProductId { get; init; }
    public string? ProductSellerCode { get; init; }
    public string? ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal? Discount { get; init; }
    public decimal? VatRate { get; init; }
    public string? Status { get; init; }
    public N11ShipmentDto? Shipment { get; init; }
}

public record N11BuyerDto(string? FirstName, string? LastName, string? Email);
public record N11AddressDto(string? City, string? District, string? FullAddress, string? PostalCode);
public record N11ShipmentDto(string? CompanyName, string? TrackingNumber, string? ShipmentCode);
