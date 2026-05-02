using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Options;

namespace Entegrasyon.Business.Concrete.Auth;

public class BatchTokenService(IOptions<BatchTokenOptions> options) : IBatchTokenService
{
    private readonly byte[] signingKey = Encoding.UTF8.GetBytes(
        options.Value.SigningKey ?? throw new InvalidOperationException("BatchToken:SigningKey is not configured."));

    public string Generate(BatchTokenPayload payload)
    {
        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(payload);
        var payloadEncoded = Base64UrlEncode(payloadJson);
        var signature = ComputeSignature(payloadEncoded);
        return $"{payloadEncoded}.{signature}";
    }

    public BatchTokenValidationResult Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new BatchTokenValidationResult(false, null, "Token is empty.");

        var parts = token.Split('.');
        if (parts.Length != 2)
            return new BatchTokenValidationResult(false, null, "Token format is invalid.");

        var (payloadPart, signaturePart) = (parts[0], parts[1]);

        var expectedSignature = ComputeSignature(payloadPart);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signaturePart),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return new BatchTokenValidationResult(false, null, "Signature mismatch.");
        }

        BatchTokenPayload? payload;
        try
        {
            var payloadJson = Base64UrlDecode(payloadPart);
            payload = JsonSerializer.Deserialize<BatchTokenPayload>(payloadJson);
        }
        catch (Exception ex)
        {
            return new BatchTokenValidationResult(false, null, $"Payload decode failed: {ex.Message}");
        }

        if (payload is null)
            return new BatchTokenValidationResult(false, null, "Payload is null.");

        if (payload.ExpiresAt < DateTimeOffset.UtcNow)
            return new BatchTokenValidationResult(false, null, "Token expired.");

        return new BatchTokenValidationResult(true, payload, null);
    }

    private string ComputeSignature(string payloadEncoded)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payloadEncoded);
        var hmac = HMACSHA256.HashData(signingKey, payloadBytes);
        return Base64UrlEncode(hmac);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
