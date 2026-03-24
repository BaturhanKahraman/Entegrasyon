using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.POS;

namespace Entegrasyon.Entity.Dtos.POS;

public sealed record POSTransactionDto(
    long POSSessionId,
    MakeSaleDto Sale,
    PaymentMethod PaymentMethod,
    decimal CashReceived,
    string? CardAuthCode);
