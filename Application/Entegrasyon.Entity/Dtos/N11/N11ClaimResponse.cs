namespace Entegrasyon.Entity.Dtos.N11;

public record N11ClaimCancelDto
{
    public long ClaimCancelId { get; init; }
    public string? Status { get; init; }
    public string? OrderNumber { get; init; }
    public string? ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string? CancelReasonType { get; init; }
    public string? CancelReasonDescription { get; init; }
    public string? BuyerName { get; init; }
    public string? BuyerEmail { get; init; }
    public DateTimeOffset? RequestDate { get; init; }
}

public record N11ClaimReturnDto
{
    public long ClaimReturnId { get; init; }
    public string? Status { get; init; }
    public string? OrderNumber { get; init; }
    public string? ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal FinalPrice { get; init; }
    public string? ReturnReasonType { get; init; }
    public string? ReturnReasonDescription { get; init; }
    public string? BuyerName { get; init; }
    public string? BuyerEmail { get; init; }
    public DateTimeOffset? RequestDate { get; init; }
    public string? ShipmentCompany { get; init; }
    public string? TrackingNumber { get; init; }
}

public record N11ReasonTypeDto(long Id, string Value);

public record N11ClaimExchangeDto
{
    public long ClaimExchangeId { get; init; }
    public string? Status { get; init; }
    public string? OrderNumber { get; init; }
    public string? ProductName { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal FinalPrice { get; init; }
    public string? ExchangeReasonType { get; init; }
    public string? ExchangeReasonDescription { get; init; }
    public string? BuyerName { get; init; }
    public string? BuyerEmail { get; init; }
    public DateTimeOffset? RequestDate { get; init; }
}
