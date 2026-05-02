using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.Devices;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Security;

public class DeviceInviteCodeManagerTests : BaseTest
{
    private readonly IDeviceInviteCodeManager sut;
    private readonly List<DeviceInviteCode> codes;

    public DeviceInviteCodeManagerTests()
    {
        codes = [];
        mockIntegrationDbContext.Setup(c => c.DeviceInviteCodes).ReturnsDbSet(codes);
        mockIntegrationDbContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mockLogger = new Mock<ILogger<DeviceInviteCodeManager>>();
        sut = new DeviceInviteCodeManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task IssueAsync_ReturnsCodeWithFutureExpiry()
    {
        var result = await sut.IssueAsync(1, Guid.NewGuid());

        result.Success.Should().BeTrue();
        result.Data.Code.Should().NotBeNullOrEmpty();
        result.Data.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        result.Data.TenantId.Should().Be(1);
        result.Data.RedeemedAt.Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_GeneratesUniqueCodesAcrossCalls()
    {
        var r1 = await sut.IssueAsync(1, Guid.NewGuid());
        var r2 = await sut.IssueAsync(1, Guid.NewGuid());

        r1.Data.Code.Should().NotBe(r2.Data.Code);
    }

    [Fact]
    public async Task RedeemAsync_WithValidCode_MarksRedeemedAndReturnsCode()
    {
        var code = new DeviceInviteCode
        {
            Id = 1,
            TenantId = 5,
            Code = "ENT-VALIDCODE",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        codes.Add(code);

        var result = await sut.RedeemAsync("ENT-VALIDCODE", deviceId: 42);

        result.Success.Should().BeTrue();
        result.Data.TenantId.Should().Be(5);
        code.RedeemedAt.Should().NotBeNull();
        code.RedeemedByDeviceId.Should().Be(42);
    }

    [Fact]
    public async Task RedeemAsync_WithUnknownCode_ReturnsError()
    {
        var result = await sut.RedeemAsync("ENT-UNKNOWN", deviceId: 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_WithExpiredCode_ReturnsError()
    {
        codes.Add(new DeviceInviteCode
        {
            Id = 1,
            TenantId = 5,
            Code = "ENT-EXPIRED",
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1)
        });

        var result = await sut.RedeemAsync("ENT-EXPIRED", deviceId: 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_WithAlreadyRedeemedCode_ReturnsError()
    {
        codes.Add(new DeviceInviteCode
        {
            Id = 1,
            TenantId = 5,
            Code = "ENT-USED",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RedeemedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            RedeemedByDeviceId = 10
        });

        var result = await sut.RedeemAsync("ENT-USED", deviceId: 2);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyUnredeemedUnexpiredForTenant()
    {
        codes.Add(new DeviceInviteCode { TenantId = 1, Code = "A", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        codes.Add(new DeviceInviteCode { TenantId = 1, Code = "B", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), RedeemedAt = DateTimeOffset.UtcNow });
        codes.Add(new DeviceInviteCode { TenantId = 1, Code = "C", ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) });
        codes.Add(new DeviceInviteCode { TenantId = 2, Code = "D", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });

        var result = await sut.GetActiveAsync(1);

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("A");
    }
}
