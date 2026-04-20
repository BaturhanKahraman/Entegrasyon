using System.Text.Json;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Notifications;

/// <summary>
/// Task 0.12: OutboxDispatcher — LISTEN/NOTIFY + retry + dead-letter doğrulama.
///
/// NOT: Bu makine Fedora rootless podman ile çalıştığından Testcontainers
/// çalıştırılamaz (bkz. reference_podman_testcontainers.md). Bu testler CI
/// ortamında (Docker daemon mevcut makinelerde) çalışacak şekilde yazılmıştır.
///
/// DI Kısıtı: IntegrationTestBase, host başlatılmadan önce ek handler kaydına
/// izin vermez. Bu nedenle handler-based doğrulama yerine sadece outbox row
/// state geçişleri doğrulanır: Pending→Completed, Pending→Processing→retry,
/// RetryCount=4+1 → Failed + DeadLetter. Handler yokken OutboxDispatcher
/// InvalidOperationException fırlatmaz — yalnızca işlemi tamamlar (0 handler
/// için başarılı sayılır).
/// </summary>
public class OutboxDispatcherTests : IntegrationTestBase
{
    public OutboxDispatcherTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static string SerializeEvent<T>(T evt) => JsonSerializer.Serialize(evt, _jsonOpts);

    private NotificationOutbox MakePendingRow(string eventType, string payloadJson, int retryCount = 0)
        => new()
        {
            EventType = eventType,
            PayloadJson = payloadJson,
            Status = OutboxStatus.Pending,
            TenantId = 1,
            RetryCount = retryCount
        };

    // ─── Tests ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Pending row without any registered handler — OutboxDispatcher should
    /// mark row as Completed (0 handlers invoked successfully).
    /// </summary>
    [Fact]
    public async Task PendingRow_NoHandlerRegistered_StatusCompleted()
    {
        // Arrange
        await using var db = CreateDbContext();
        var evt = new ProductAddedEvent(Guid.NewGuid(), "Test Ürün", Guid.NewGuid()) { TenantId = 1 };
        var row = MakePendingRow("ProductAddedEvent", SerializeEvent(evt));
        db.NotificationOutbox.Add(row);
        await db.SaveChangesAsync();
        var rowId = row.Id;

        // Act
        var dispatcher = GetService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        // Assert
        await using var db2 = CreateDbContext();
        var saved = await db2.NotificationOutbox.AsNoTracking().SingleAsync(x => x.Id == rowId);
        saved.Status.Should().Be(OutboxStatus.Completed);
        saved.ProcessedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Row with unknown event type — dispatcher should increment retry count
    /// (InvalidOperationException: unknown type) and set next retry.
    /// </summary>
    [Fact]
    public async Task UnknownEventType_RetryCountIncremented_NextRetrySet()
    {
        // Arrange
        await using var db = CreateDbContext();
        var row = MakePendingRow("NonExistentEventType_XYZ_12345", "{}", retryCount: 0);
        db.NotificationOutbox.Add(row);
        await db.SaveChangesAsync();
        var rowId = row.Id;

        // Act
        var dispatcher = GetService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        // Assert
        await using var db2 = CreateDbContext();
        var saved = await db2.NotificationOutbox.AsNoTracking().SingleAsync(x => x.Id == rowId);
        saved.RetryCount.Should().Be(1);
        saved.Status.Should().Be(OutboxStatus.Pending);
        saved.NextRetryAt.Should().NotBeNull("exponential backoff scheduled");
        saved.LastError.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Row at MaxRetryCount - 1 (RetryCount=4) with unknown type fails →
    /// should move to dead letter and set status Failed.
    /// </summary>
    [Fact]
    public async Task FifthFailure_MovedToDeadLetter_StatusFailed()
    {
        // Arrange
        await using var db = CreateDbContext();
        // RetryCount = MaxRetryCount - 1 = 4, so next failure pushes it over
        var row = MakePendingRow("NonExistentEventType_DeadLetter_99999", "{}", retryCount: 4);
        db.NotificationOutbox.Add(row);
        await db.SaveChangesAsync();
        var rowId = row.Id;

        // Act
        var dispatcher = GetService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        // Assert
        await using var db2 = CreateDbContext();
        var saved = await db2.NotificationOutbox.AsNoTracking().SingleAsync(x => x.Id == rowId);
        saved.Status.Should().Be(OutboxStatus.Failed);
        saved.RetryCount.Should().Be(5);

        var deadLetter = await db2.DeadLetterOutbox
            .AsNoTracking()
            .SingleAsync(x => x.OriginalOutboxId == rowId);
        deadLetter.TenantId.Should().Be(1);
        deadLetter.EventType.Should().Be("NonExistentEventType_DeadLetter_99999");
        deadLetter.TotalRetryCount.Should().Be(5);
        deadLetter.FinalError.Should().NotBeNullOrEmpty();
        deadLetter.MovedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Empty outbox — DispatchPendingAsync should complete without error.
    /// </summary>
    [Fact]
    public async Task EmptyOutbox_DispatchPendingAsync_CompletesCleanly()
    {
        // Act + Assert: no exception
        var dispatcher = GetService<OutboxDispatcher>();
        var act = async () => await dispatcher.DispatchPendingAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
