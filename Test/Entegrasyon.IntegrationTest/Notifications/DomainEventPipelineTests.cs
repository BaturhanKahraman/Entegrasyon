using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Notifications;

/// <summary>
/// Task 2.4: Uçtan uca domain-event pipeline entegrasyon testi.
///
/// Doğrulanan akış:
///   dbContext.AddDomainEvent(ProductAddedEvent)
///     → SaveChangesAsync → NotificationOutbox satırı (Pending)
///     → OutboxDispatcher.DispatchPendingAsync
///     → IDomainEventHandler&lt;ProductAddedEvent&gt; (ProductAddedNotificationHandler)
///     → INotificationRecipientResolver.ResolveByPermissionAsync("products.view")
///     → INotificationManager.SendNotification
///     → Notification satırı + NotificationsUsers junction satırı
///
/// NOT: Bu makine Fedora rootless podman ile çalıştığından Testcontainers
/// çalıştırılamaz (bkz. reference_podman_testcontainers.md). Bu test CI
/// ortamında (Docker daemon mevcut makinelerde) çalışacak şekilde yazılmıştır.
/// </summary>
public class DomainEventPipelineTests : IntegrationTestBase
{
    public DomainEventPipelineTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task AddProductAddedEvent_DispatcherRuns_NotificationCreatedForRecipient()
    {
        // ─── Arrange ─────────────────────────────────────────────────────────

        var actorId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Seed actor + recipient users
        await using (var db = CreateDbContext())
        {
            db.Users.Add(new ApplicationUser
            {
                Id = actorId,
                Name = "Actor",
                Surname = "User",
                FullName = "Actor User",
                UserName = $"actor-{actorId:N}",
                NormalizedUserName = $"ACTOR-{actorId:N}".ToUpperInvariant(),
                Email = $"actor-{actorId:N}@test.com",
                NormalizedEmail = $"ACTOR-{actorId:N}@TEST.COM".ToUpperInvariant(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

            db.Users.Add(new ApplicationUser
            {
                Id = recipientId,
                Name = "Recipient",
                Surname = "User",
                FullName = "Recipient User",
                UserName = $"recipient-{recipientId:N}",
                NormalizedUserName = $"RECIPIENT-{recipientId:N}".ToUpperInvariant(),
                Email = $"recipient-{recipientId:N}@test.com",
                NormalizedEmail = $"RECIPIENT-{recipientId:N}@TEST.COM".ToUpperInvariant(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();

            // Grant recipient "products.view" permission via direct UsersClaims
            db.Set<UsersClaims>().Add(new UsersClaims
            {
                ApplicationUserId = recipientId,
                Permission = "products.view"
            });

            await db.SaveChangesAsync();
        }

        // Add domain event — outbox row is written atomically in the same tx
        await using (var db = CreateDbContext())
        {
            var evt = new ProductAddedEvent(productId, "Pipeline Test Ürün", actorId)
            {
                TenantId = 1
            };
            db.AddDomainEvent(evt);
            await db.SaveChangesAsync();
        }

        // ─── Act ─────────────────────────────────────────────────────────────

        var dispatcher = GetService<OutboxDispatcher>();
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        // ─── Assert ──────────────────────────────────────────────────────────

        await using var db2 = CreateDbContext();

        // 1. Outbox row must be Completed
        var outboxRow = await db2.NotificationOutbox
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.EventType == "ProductAddedEvent" &&
                x.TenantId == 1 &&
                x.PayloadJson.Contains("Pipeline Test Ürün"));

        outboxRow.Should().NotBeNull("outbox satırı SaveChangesAsync tarafından yazılmış olmalı");
        outboxRow!.Status.Should().Be(OutboxStatus.Completed);
        outboxRow.ProcessedAt.Should().NotBeNull();

        // 2. Notification row must exist with correct header/content/meta
        var notif = await db2.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Header == "Yeni ürün eklendi" &&
                                      n.ActionUrl != null &&
                                      n.ActionUrl.Contains(productId.ToString()));

        notif.Should().NotBeNull("ProductAddedNotificationHandler bir Notification satırı oluşturmuş olmalı");
        notif!.Content.Should().Contain("Pipeline Test Ürün");
        notif.Category.Should().Be(NotificationCategory.Urun);
        notif.Severity.Should().Be(NotificationSeverity.Info);
        notif.ActionUrl.Should().Be($"/products/{productId}");

        // 3. NotificationsUsers junction rows: recipient included, actor excluded
        var junctions = await db2.Set<NotificationsUsers>()
            .AsNoTracking()
            .Where(nu => nu.NotificationId == notif.Id)
            .ToListAsync();

        junctions.Should().NotBeEmpty("en az bir alıcı kaydı olmalı");
        junctions.Should().Contain(nu => nu.ApplicationUserId == recipientId,
            "recipient 'products.view' iznine sahip olduğu için bildirimi almalı");
        junctions.Should().NotContain(nu => nu.ApplicationUserId == actorId,
            "actor (event'i tetikleyen) bildirim almamalı — handler tarafından filtrelenir");
    }
}
