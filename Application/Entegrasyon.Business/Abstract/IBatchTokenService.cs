namespace Entegrasyon.Business.Abstract;

public record BatchTokenPayload(
    Guid BatchId,
    int TenantId,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    string Nonce);

public record BatchTokenValidationResult(
    bool IsValid,
    BatchTokenPayload? Payload,
    string? Error);

public interface IBatchTokenService
{
    string Generate(BatchTokenPayload payload);
    BatchTokenValidationResult Validate(string token);
}
