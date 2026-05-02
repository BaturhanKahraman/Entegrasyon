using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.Devices;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Security;

public class DeviceManagerTests : BaseTest
{
    private readonly IDeviceManager sut;
    private readonly List<Device> devices;

    public DeviceManagerTests()
    {
        devices = [];
        mockIntegrationDbContext.Setup(c => c.Devices).ReturnsDbSet(devices);
        mockIntegrationDbContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mockLogger = new Mock<ILogger<DeviceManager>>();
        sut = new DeviceManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidInput_ReturnsSuccessAndPlainKey()
    {
        var result = await sut.RegisterAsync(1, "Kasa-1", "linux", "host-1");

        result.Success.Should().BeTrue();
        result.Data.PlainKey.Should().NotBeNullOrEmpty();
        result.Data.PlainKey.Should().StartWith("dev_");
        result.Data.Device.Name.Should().Be("Kasa-1");
        result.Data.Device.TenantId.Should().Be(1);
        result.Data.Device.OS.Should().Be("linux");
        result.Data.Device.Hostname.Should().Be("host-1");
        result.Data.Device.IsRevoked.Should().BeFalse();
        result.Data.Device.KeyHash.Should().NotBe(result.Data.PlainKey);
        result.Data.Device.KeyPrefix.Should().StartWith("dev_");
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyName_ReturnsError()
    {
        var result = await sut.RegisterAsync(1, "", "linux", "h");

        result.Success.Should().BeFalse();
        result.Should().BeOfType<ErrorDataResult<(Device, string)>>();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateNameForSameTenant_ReturnsError()
    {
        devices.Add(new Device { TenantId = 1, Name = "Kasa-1", IsRevoked = false });

        var result = await sut.RegisterAsync(1, "Kasa-1", "linux", "h");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_AllowsSameNameForDifferentTenant()
    {
        devices.Add(new Device { TenantId = 2, Name = "Kasa-1", IsRevoked = false });

        var result = await sut.RegisterAsync(1, "Kasa-1", "linux", "h");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateKeyAsync_WithValidKey_ReturnsDevice()
    {
        var registration = await sut.RegisterAsync(1, "Kasa-1", "linux", "h");
        var plainKey = registration.Data.PlainKey;
        devices.Add(registration.Data.Device);

        var result = await sut.ValidateKeyAsync(plainKey);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Kasa-1");
    }

    [Fact]
    public async Task ValidateKeyAsync_WithInvalidKey_ReturnsNull()
    {
        var result = await sut.ValidateKeyAsync("dev_bogus_key_abc");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateKeyAsync_WithRevokedDevice_ReturnsNull()
    {
        var registration = await sut.RegisterAsync(1, "Kasa-1", "linux", "h");
        var plainKey = registration.Data.PlainKey;
        var device = registration.Data.Device;
        device.IsRevoked = true;
        devices.Add(device);

        var result = await sut.ValidateKeyAsync(plainKey);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateKeyAsync_WithEmptyKey_ReturnsNull()
    {
        var result = await sut.ValidateKeyAsync("");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyDevicesForTenant()
    {
        devices.Add(new Device { Id = 1, TenantId = 1, Name = "A" });
        devices.Add(new Device { Id = 2, TenantId = 2, Name = "B" });
        devices.Add(new Device { Id = 3, TenantId = 1, Name = "C" });

        var result = await sut.GetAllAsync(1);

        result.Should().HaveCount(2);
        result.Select(d => d.Name).Should().Contain(["A", "C"]);
    }

    [Fact]
    public async Task RevokeAsync_WithInvalidId_ReturnsError()
    {
        var result = await sut.RevokeAsync(999);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWithInviteCodeAsync_WithValidCode_RegistersAndRedeems()
    {
        var invite = new DeviceInviteCode
        {
            Id = 7, TenantId = 5, Code = "ENT-XYZ",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        var inviteList = new List<DeviceInviteCode> { invite };
        mockIntegrationDbContext.Setup(c => c.DeviceInviteCodes).ReturnsDbSet(inviteList);

        var result = await sut.RegisterWithInviteCodeAsync("ENT-XYZ", "Kasa-1", "linux", "h");

        result.Success.Should().BeTrue();
        result.Data.Device.TenantId.Should().Be(5);
        result.Data.Device.Name.Should().Be("Kasa-1");
        result.Data.PlainKey.Should().StartWith("dev_");
        invite.RedeemedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterWithInviteCodeAsync_WithExpiredCode_Fails()
    {
        var inviteList = new List<DeviceInviteCode>
        {
            new() { Id = 7, TenantId = 5, Code = "ENT-EXP",
                    ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) }
        };
        mockIntegrationDbContext.Setup(c => c.DeviceInviteCodes).ReturnsDbSet(inviteList);

        var result = await sut.RegisterWithInviteCodeAsync("ENT-EXP", "Kasa-1", "linux", "h");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWithInviteCodeAsync_WithUsedCode_Fails()
    {
        var inviteList = new List<DeviceInviteCode>
        {
            new() { Id = 7, TenantId = 5, Code = "ENT-USED",
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                    RedeemedAt = DateTimeOffset.UtcNow.AddMinutes(-1) }
        };
        mockIntegrationDbContext.Setup(c => c.DeviceInviteCodes).ReturnsDbSet(inviteList);

        var result = await sut.RegisterWithInviteCodeAsync("ENT-USED", "Kasa-1", "linux", "h");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterWithInviteCodeAsync_WithUnknownCode_Fails()
    {
        mockIntegrationDbContext.Setup(c => c.DeviceInviteCodes).ReturnsDbSet(new List<DeviceInviteCode>());

        var result = await sut.RegisterWithInviteCodeAsync("ENT-NOPE", "Kasa-1", "linux", "h");

        result.Success.Should().BeFalse();
    }
}
