namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record SalePaymentDto(
    int PaymentMethodId,
    decimal Amount,
    decimal? CashReceived,
    string? CardAuthCode);
