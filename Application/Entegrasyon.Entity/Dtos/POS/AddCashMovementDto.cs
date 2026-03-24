using Entegrasyon.Entity.POS;

namespace Entegrasyon.Entity.Dtos.POS;

public sealed record AddCashMovementDto(
    long POSSessionId,
    CashMovementType MovementType,
    decimal Amount,
    string? Reason);
