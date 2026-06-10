using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.Entity.Dtos.Settings;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontEmailServiceTests
{
    private readonly Mock<IApplicationSettingManager> _mockSettingManager = new();
    private readonly Mock<ILogger<StorefrontEmailService>> _mockLogger = new();

    private StorefrontEmailService CreateService()
        => new(_mockSettingManager.Object, _mockLogger.Object);

    private void SetupEmptySmtpSettings()
    {
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync("E-posta Ayarlar\u0131"))
            .ReturnsAsync(new List<ApplicationSettingDto>());
    }

    private void SetupValidSmtpSettings()
    {
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync("E-posta Ayarlar\u0131"))
            .ReturnsAsync(new List<ApplicationSettingDto>
            {
                new() { Key = "SmtpHost", Value = "smtp.test.com" },
                new() { Key = "SmtpPort", Value = "587" },
                new() { Key = "SmtpEnableSsl", Value = "true" },
                new() { Key = "SmtpUsername", Value = "user@test.com" },
                new() { Key = "SmtpPassword", Value = "pass" },
                new() { Key = "SmtpFromAddress", Value = "noreply@test.com" },
                new() { Key = "SmtpFromDisplayName", Value = "Test Mağaza" }
            });
    }

    [Fact]
    public async Task SendAsync_NoSmtpSettings_ReturnsError()
    {
        SetupEmptySmtpSettings();
        var service = CreateService();

        var result = await service.SendAsync("test@test.com", "Subject", "<p>Body</p>");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SMTP");
    }

    [Fact]
    public async Task SendAsync_MissingSmtpHost_ReturnsError()
    {
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync("E-posta Ayarlar\u0131"))
            .ReturnsAsync(new List<ApplicationSettingDto>
            {
                new() { Key = "SmtpFromAddress", Value = "noreply@test.com" }
            });
        var service = CreateService();

        var result = await service.SendAsync("test@test.com", "Subject", "<p>Body</p>");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SMTP");
    }

    [Fact]
    public async Task SendAsync_MissingFromAddress_ReturnsError()
    {
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync("E-posta Ayarlar\u0131"))
            .ReturnsAsync(new List<ApplicationSettingDto>
            {
                new() { Key = "SmtpHost", Value = "smtp.test.com" }
            });
        var service = CreateService();

        var result = await service.SendAsync("test@test.com", "Subject", "<p>Body</p>");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SMTP");
    }

    [Fact]
    public async Task SendAsync_SmtpConnectionFails_ReturnsErrorGracefully()
    {
        SetupValidSmtpSettings();
        var service = CreateService();

        // Will fail because smtp.test.com doesn't exist, but should return error gracefully
        var result = await service.SendAsync("test@test.com", "Subject", "<p>Body</p>");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email gonderilemedi");
    }

    [Fact]
    public async Task SendEmailVerificationAsync_NoSmtpConfig_ReturnsError()
    {
        SetupEmptySmtpSettings();
        var service = CreateService();

        var result = await service.SendEmailVerificationAsync(
            "test@test.com", "Ali", "token123", "Test Mağaza", "test.com");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendPasswordResetAsync_NoSmtpConfig_ReturnsError()
    {
        SetupEmptySmtpSettings();
        var service = CreateService();

        var result = await service.SendPasswordResetAsync(
            "test@test.com", "Ali", "resettoken", "Test Mağaza", "test.com");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendWelcomeAsync_NoSmtpConfig_ReturnsError()
    {
        SetupEmptySmtpSettings();
        var service = CreateService();

        var result = await service.SendWelcomeAsync(
            "test@test.com", "Ali", "Test Mağaza");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendOrderConfirmationAsync_NoSmtpConfig_ReturnsError()
    {
        SetupEmptySmtpSettings();
        var service = CreateService();

        var result = await service.SendOrderConfirmationAsync(
            "test@test.com", "Ali", "SF-20260326-ABC12345", 199.90m, "Test Mağaza", "test.com");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_SettingManagerThrows_ReturnsErrorGracefully()
    {
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));
        var service = CreateService();

        var result = await service.SendAsync("test@test.com", "Subject", "<p>Body</p>");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email gonderilemedi");
    }
}
