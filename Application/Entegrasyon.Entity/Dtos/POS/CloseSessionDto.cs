namespace Entegrasyon.Entity.Dtos.POS;

public sealed record CloseSessionDto(
    long SessionId,
    decimal ClosingCash);
