using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Stock;
using Entegrasyon.Entity.Stock;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Stock;

public class NegativeStockIncidentManagerTests : BaseTest
{
    private readonly INegativeStockIncidentManager sut;
    private readonly List<NegativeStockIncident> incidents;

    public NegativeStockIncidentManagerTests()
    {
        incidents = [];
        mockIntegrationDbContext.Setup(c => c.NegativeStockIncidents).ReturnsDbSet(incidents);
        mockIntegrationDbContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mockLogger = new Mock<ILogger<NegativeStockIncidentManager>>();
        sut = new NegativeStockIncidentManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_RecordsIncidentWithDetectedAt()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var dto = new CreateIncidentDto(
            TenantId: 5, BranchOfficeId: 1, ProductVariantId: Guid.NewGuid(),
            Quantity: 3, StockBefore: 2, StockAfter: -1,
            TriggeringSource: "OfflinePos",
            TriggeringReferenceId: "uuid-abc",
            TriggeringSaleId: Guid.NewGuid());

        var result = await sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        result.Data.TenantId.Should().Be(5);
        result.Data.Quantity.Should().Be(3);
        result.Data.StockAfter.Should().Be(-1);
        result.Data.TriggeringSource.Should().Be("OfflinePos");
        result.Data.DetectedAt.Should().BeAfter(before);
        result.Data.Resolution.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidTenantId()
    {
        var dto = new CreateIncidentDto(0, 1, Guid.NewGuid(), 1, 0, -1, "OfflinePos", null, null);
        var result = await sut.CreateAsync(dto);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptySource()
    {
        var dto = new CreateIncidentDto(1, 1, Guid.NewGuid(), 1, 0, -1, "", null, null);
        var result = await sut.CreateAsync(dto);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyUnresolvedForTenant()
    {
        incidents.Add(new NegativeStockIncident { Id = 1, TenantId = 1, Resolution = null });
        incidents.Add(new NegativeStockIncident { Id = 2, TenantId = 1, Resolution = NegativeStockResolution.StockAdded, ResolvedAt = DateTimeOffset.UtcNow });
        incidents.Add(new NegativeStockIncident { Id = 3, TenantId = 2, Resolution = null });
        incidents.Add(new NegativeStockIncident { Id = 4, TenantId = 1, Resolution = null });

        var result = await sut.GetActiveAsync(1);

        result.Should().HaveCount(2);
        result.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1, 4 });
    }

    [Fact]
    public async Task GetActiveCountAsync_ReturnsCountForTenant()
    {
        incidents.Add(new NegativeStockIncident { TenantId = 1, Resolution = null });
        incidents.Add(new NegativeStockIncident { TenantId = 1, Resolution = null });
        incidents.Add(new NegativeStockIncident { TenantId = 1, Resolution = NegativeStockResolution.StockAdded });
        incidents.Add(new NegativeStockIncident { TenantId = 2, Resolution = null });

        var count = await sut.GetActiveCountAsync(1);

        count.Should().Be(2);
    }

    [Fact]
    public async Task ResolveAsync_MarksResolvedWithUserAndTimestamp()
    {
        var incident = new NegativeStockIncident { Id = 7, TenantId = 1, Resolution = null };
        incidents.Add(incident);

        var userId = Guid.NewGuid();
        var dto = new ResolveIncidentDto(NegativeStockResolution.StockAdded, userId, "Mal sayım hatasıydı");

        var result = await sut.ResolveAsync(7, tenantId: 1, dto);

        result.Success.Should().BeTrue();
        incident.Resolution.Should().Be(NegativeStockResolution.StockAdded);
        incident.ResolvedByUserId.Should().Be(userId);
        incident.ResolvedAt.Should().NotBeNull();
        incident.Notes.Should().Be("Mal sayım hatasıydı");
    }

    [Fact]
    public async Task ResolveAsync_FailsWhenAlreadyResolved()
    {
        incidents.Add(new NegativeStockIncident
        {
            Id = 7, TenantId = 1,
            Resolution = NegativeStockResolution.StockAdded,
            ResolvedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        });

        var dto = new ResolveIncidentDto(NegativeStockResolution.OnlineSaleCancelled, Guid.NewGuid(), null);

        var result = await sut.ResolveAsync(7, tenantId: 1, dto);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveAsync_FailsWhenWrongTenant()
    {
        incidents.Add(new NegativeStockIncident { Id = 7, TenantId = 1, Resolution = null });

        var dto = new ResolveIncidentDto(NegativeStockResolution.StockAdded, Guid.NewGuid(), null);

        var result = await sut.ResolveAsync(7, tenantId: 999, dto);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsForCorrectTenant()
    {
        incidents.Add(new NegativeStockIncident { Id = 7, TenantId = 1 });
        incidents.Add(new NegativeStockIncident { Id = 8, TenantId = 2 });

        var ok = await sut.GetByIdAsync(7, tenantId: 1);
        var wrongTenant = await sut.GetByIdAsync(8, tenantId: 1);

        ok.Success.Should().BeTrue();
        wrongTenant.Success.Should().BeFalse();
    }
}
