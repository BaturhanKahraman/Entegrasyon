using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.Settings;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Business;

public class BarcodeScannerServiceTests : BaseTest
{
    private readonly Mock<IApplicationSettingManager> _mockSettingManager;
    private readonly BarcodeScannerService _sut;

    public BarcodeScannerServiceTests()
    {
        _mockSettingManager = new Mock<IApplicationSettingManager>();
        _sut = new BarcodeScannerService(_mockSettingManager.Object);
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsDefaults_WhenNoSettingsExist()
    {
        // Arrange
        _mockSettingManager
            .Setup(x => x.GetSettingAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationSettingDto?)null);

        // Act
        var result = await _sut.GetConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.Enabled.Should().BeTrue();
        result.Timeout.Should().Be(100);
        result.MinLength.Should().Be(6);
        result.DefaultAction.Should().Be("SalesAdd");
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsStoredValues_WhenSettingsExist()
    {
        // Arrange
        _mockSettingManager
            .Setup(x => x.GetSettingAsync("BarcodeScanner.Enabled"))
            .ReturnsAsync(new ApplicationSettingDto { Key = "BarcodeScanner.Enabled", Value = "false", ValueType = SettingValueType.Boolean });
        _mockSettingManager
            .Setup(x => x.GetSettingAsync("BarcodeScanner.Timeout"))
            .ReturnsAsync(new ApplicationSettingDto { Key = "BarcodeScanner.Timeout", Value = "200", ValueType = SettingValueType.Integer });
        _mockSettingManager
            .Setup(x => x.GetSettingAsync("BarcodeScanner.MinLength"))
            .ReturnsAsync(new ApplicationSettingDto { Key = "BarcodeScanner.MinLength", Value = "8", ValueType = SettingValueType.Integer });
        _mockSettingManager
            .Setup(x => x.GetSettingAsync("BarcodeScanner.DefaultAction"))
            .ReturnsAsync(new ApplicationSettingDto { Key = "BarcodeScanner.DefaultAction", Value = "ProductSearch", ValueType = SettingValueType.String });

        // Act
        var result = await _sut.GetConfigAsync();

        // Assert
        result.Enabled.Should().BeFalse();
        result.Timeout.Should().Be(200);
        result.MinLength.Should().Be(8);
        result.DefaultAction.Should().Be("ProductSearch");
    }

    [Fact]
    public async Task UpdateConfigAsync_CallsUpdateSettings_WithCorrectValues()
    {
        // Arrange
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync("Barkod Okuyucu"))
            .ReturnsAsync([
                new ApplicationSettingDto { Id = 9, Key = "BarcodeScanner.Enabled", Value = "true", ValueType = SettingValueType.Boolean },
                new ApplicationSettingDto { Id = 10, Key = "BarcodeScanner.Timeout", Value = "100", ValueType = SettingValueType.Integer },
                new ApplicationSettingDto { Id = 11, Key = "BarcodeScanner.MinLength", Value = "6", ValueType = SettingValueType.Integer },
                new ApplicationSettingDto { Id = 12, Key = "BarcodeScanner.DefaultAction", Value = "SalesAdd", ValueType = SettingValueType.String },
            ]);

        _mockSettingManager
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<List<UpdateApplicationSettingDto>>()))
            .ReturnsAsync(true);

        var config = new BarcodeScannerConfig
        {
            Enabled = false,
            Timeout = 150,
            MinLength = 10,
            DefaultAction = "NavigateToSales"
        };

        // Act
        var result = await _sut.UpdateConfigAsync(config);

        // Assert
        result.Success.Should().BeTrue();
        _mockSettingManager.Verify(x => x.UpdateSettingsAsync(It.Is<List<UpdateApplicationSettingDto>>(
            list => list.Count == 4)), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigAsync_ReturnsFailure_WhenNoSettingsFound()
    {
        // Arrange
        _mockSettingManager
            .Setup(x => x.GetSettingsByGroupAsync("Barkod Okuyucu"))
            .ReturnsAsync([]);

        var config = new BarcodeScannerConfig();

        // Act
        var result = await _sut.UpdateConfigAsync(config);

        // Assert
        result.Success.Should().BeFalse();
    }
}
