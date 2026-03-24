namespace Entegrasyon.Entity.Dtos.POS;

public sealed record OpenSessionDto(
    int BranchOfficeId,
    Guid CashierId,
    decimal OpeningCash,
    string? TerminalId);
