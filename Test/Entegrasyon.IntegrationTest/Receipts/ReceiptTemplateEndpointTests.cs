using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Receipts;

[Trait("Category", "Integration")]
public class ReceiptTemplateEndpointTests : IntegrationTestBase
{
    public ReceiptTemplateEndpointTests(PostgreSqlFixture pg, WireMockFixture wm) : base(pg, wm) { }

    [Fact]
    public async Task GetAsync_ReturnsSeedDefault()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();

        var result = await mgr.GetAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ThermalJson.Should().Contain("return_code");
        result.Data.A4Json.Should().Contain("hl");
    }

    [Fact]
    public async Task UpdateAsync_InvalidThermalJson_ReturnsError()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();

        var result = await mgr.UpdateAsync(new UpdateReceiptTemplateDto("not json", "{}", 120, "", "", ""));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Thermal JSON");
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesAndPersists()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();

        var result = await mgr.UpdateAsync(new UpdateReceiptTemplateDto("[]", "{}", 100, "Test Mağaza", "Cadde 1", "0555"));
        result.Success.Should().BeTrue();

        var reloaded = await mgr.GetAsync();
        reloaded.Data!.StoreName.Should().Be("Test Mağaza");
        reloaded.Data.LogoWidthPx.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_LogoWidthOutOfRange_ReturnsError()
    {
        using var scope = Services.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService<IReceiptTemplateManager>();

        var result = await mgr.UpdateAsync(new UpdateReceiptTemplateDto("[]", "{}", 50, "", "", ""));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("80-200");
    }
}
