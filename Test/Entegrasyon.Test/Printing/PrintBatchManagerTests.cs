using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Concrete.Printing;
using Entegrasyon.Entity.Printing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Printing;

public class PrintBatchManagerTests : BaseTest
{
    private readonly IPrintBatchManager sut;
    private readonly IBatchTokenService tokenService;
    private readonly List<PrintBatch> batches;
    private readonly List<PrintBatchItem> items;

    public PrintBatchManagerTests()
    {
        batches = [];
        items = [];

        mockIntegrationDbContext.Setup(c => c.PrintBatches).ReturnsDbSet(batches);
        mockIntegrationDbContext.Setup(c => c.PrintBatchItems).ReturnsDbSet(items);
        mockIntegrationDbContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var tokenOptions = Options.Create(new BatchTokenOptions
        {
            SigningKey = "unit-test-signing-key-min-32-chars-long-please"
        });
        tokenService = new BatchTokenService(tokenOptions);

        var mockLogger = new Mock<ILogger<PrintBatchManager>>();
        var mockBroadcaster = new Mock<IPrintBatchProgressBroadcaster>();
        sut = new PrintBatchManager(
            mockContextFactory.Object,
            tokenService,
            mockBroadcaster.Object,
            mockApplicationLogger.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_WithItems_ReturnsBatchAndToken()
    {
        var drafts = new List<PrintBatchItemDraft>
        {
            new(1, "ZPL", "^XA...^XZ", null, "Barkod: 869001"),
            new(2, "ZPL", "^XA...^XZ", null, "Barkod: 869002")
        };

        var result = await sut.CreateAsync(tenantId: 5, userId: Guid.NewGuid(), drafts);

        result.Success.Should().BeTrue();
        result.Data.BatchId.Should().NotBe(Guid.Empty);
        result.Data.TotalItems.Should().Be(2);
        result.Data.OneTimeBatchToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyItemList_ReturnsError()
    {
        var result = await sut.CreateAsync(1, Guid.NewGuid(), []);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_TokenIsValidatableByService()
    {
        var drafts = new List<PrintBatchItemDraft>
        {
            new(1, "ZPL", "^XA^XZ", null, "")
        };

        var result = await sut.CreateAsync(7, Guid.NewGuid(), drafts);

        var validation = tokenService.Validate(result.Data.OneTimeBatchToken);
        validation.IsValid.Should().BeTrue();
        validation.Payload!.BatchId.Should().Be(result.Data.BatchId);
        validation.Payload.TenantId.Should().Be(7);
    }

    [Fact]
    public async Task CreateAsync_TokenContainsCorrectExpiry()
    {
        var drafts = new List<PrintBatchItemDraft> { new(1, "ZPL", "^XA^XZ", null, "") };
        var before = DateTimeOffset.UtcNow;

        var result = await sut.CreateAsync(1, Guid.NewGuid(), drafts);

        var payload = tokenService.Validate(result.Data.OneTimeBatchToken).Payload!;
        payload.ExpiresAt.Should().BeAfter(before).And.BeBefore(before.AddMinutes(2));
    }

    [Fact]
    public async Task GetForDeviceAsync_WithMatchingTenant_ReturnsBatch()
    {
        var batchId = Guid.NewGuid();
        var batch = new PrintBatch
        {
            Id = batchId, TenantId = 5, TotalItems = 1,
            Status = PrintBatchStatus.Pending,
            Items =
            [
                new PrintBatchItem { Id = 1, PrintBatchId = batchId, Order = 1, ZplContent = "^XA^XZ" }
            ]
        };
        batches.Add(batch);
        items.AddRange(batch.Items);

        var result = await sut.GetForDeviceAsync(batchId, deviceId: 99, tenantId: 5);

        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(batchId);
    }

    [Fact]
    public async Task GetForDeviceAsync_WithMismatchedTenant_ReturnsError()
    {
        var batchId = Guid.NewGuid();
        batches.Add(new PrintBatch { Id = batchId, TenantId = 5 });

        var result = await sut.GetForDeviceAsync(batchId, deviceId: 99, tenantId: 9);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetForDeviceAsync_WithUnknownBatch_ReturnsError()
    {
        var result = await sut.GetForDeviceAsync(Guid.NewGuid(), deviceId: 1, tenantId: 1);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RecordItemStatusAsync_MarksItemsPrinted_AndCompletesBatch()
    {
        var batchId = Guid.NewGuid();
        var batch = new PrintBatch
        {
            Id = batchId, TenantId = 1, TotalItems = 2,
            Status = PrintBatchStatus.InProgress
        };
        var i1 = new PrintBatchItem { Id = 1, PrintBatchId = batchId, Order = 1 };
        var i2 = new PrintBatchItem { Id = 2, PrintBatchId = batchId, Order = 2 };
        batch.Items.AddRange([i1, i2]);
        batches.Add(batch);
        items.AddRange([i1, i2]);

        var updates = new List<PrintBatchItemStatusUpdate>
        {
            new(1, Printed: true, null),
            new(2, Printed: true, null)
        };

        var result = await sut.RecordItemStatusAsync(batchId, deviceId: 99, updates);

        result.Success.Should().BeTrue();
        i1.Status.Should().Be(PrintBatchItemStatus.Printed);
        i2.Status.Should().Be(PrintBatchItemStatus.Printed);
        batch.CompletedItems.Should().Be(2);
        batch.Status.Should().Be(PrintBatchStatus.Completed);
    }

    [Fact]
    public async Task RecordItemStatusAsync_WithFailure_RecordsErrorAndKeepsBatchInProgress()
    {
        var batchId = Guid.NewGuid();
        var batch = new PrintBatch
        {
            Id = batchId, TenantId = 1, TotalItems = 2,
            Status = PrintBatchStatus.InProgress
        };
        var i1 = new PrintBatchItem { Id = 1, PrintBatchId = batchId, Order = 1 };
        var i2 = new PrintBatchItem { Id = 2, PrintBatchId = batchId, Order = 2 };
        batch.Items.AddRange([i1, i2]);
        batches.Add(batch);
        items.AddRange([i1, i2]);

        var updates = new List<PrintBatchItemStatusUpdate>
        {
            new(1, Printed: true, null),
            new(2, Printed: false, "Yazıcı kağıt yok")
        };

        await sut.RecordItemStatusAsync(batchId, deviceId: 99, updates);

        i1.Status.Should().Be(PrintBatchItemStatus.Printed);
        i2.Status.Should().Be(PrintBatchItemStatus.Failed);
        i2.ErrorMessage.Should().Be("Yazıcı kağıt yok");
        batch.Status.Should().Be(PrintBatchStatus.Failed);
    }
}
