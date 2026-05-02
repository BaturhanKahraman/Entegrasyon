using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Entegrasyon.UnitTest.Security;

public class BatchTokenServiceTests
{
    private readonly IBatchTokenService service;

    public BatchTokenServiceTests()
    {
        var options = Options.Create(new BatchTokenOptions
        {
            SigningKey = "test-secret-key-minimum-32-chars-long-for-hmac"
        });
        service = new BatchTokenService(options);
    }

    [Fact]
    public void Generate_Then_Validate_ReturnsOriginalPayload()
    {
        var payload = new BatchTokenPayload(
            BatchId: Guid.NewGuid(),
            TenantId: 42,
            UserId: Guid.NewGuid(),
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(1),
            Nonce: "random-nonce-123");

        var token = service.Generate(payload);
        var result = service.Validate(token);

        result.IsValid.Should().BeTrue();
        result.Payload.Should().NotBeNull();
        result.Payload!.BatchId.Should().Be(payload.BatchId);
        result.Payload.TenantId.Should().Be(42);
        result.Payload.UserId.Should().Be(payload.UserId);
        result.Payload.Nonce.Should().Be("random-nonce-123");
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenSignatureTampered()
    {
        var payload = new BatchTokenPayload(
            Guid.NewGuid(), 1, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(1), "n");

        var token = service.Generate(payload);
        var tampered = token[..^5] + "XXXXX";

        var result = service.Validate(tampered);

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenTokenExpired()
    {
        var payload = new BatchTokenPayload(
            Guid.NewGuid(), 1, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddSeconds(-1), "n");

        var token = service.Generate(payload);
        var result = service.Validate(token);

        result.IsValid.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenMalformed()
    {
        var result = service.Validate("not-a-valid-token");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Generate_DifferentSigningKey_ProducesIncompatibleTokens()
    {
        var otherOptions = Options.Create(new BatchTokenOptions
        {
            SigningKey = "completely-different-secret-key-32chars"
        });
        var otherService = new BatchTokenService(otherOptions);

        var payload = new BatchTokenPayload(
            Guid.NewGuid(), 1, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(1), "n");

        var token = service.Generate(payload);
        var result = otherService.Validate(token);

        result.IsValid.Should().BeFalse();
    }
}
