using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Notifications;

/// <summary>
/// Task 0.9: AddDomainEvent buffer + SaveChangesAsync outbox yazımı doğrulama.
/// Her SaveChangesAsync çağrısında bekleyen domain event'ler notification_outbox
/// tablosuna aynı transaction içinde atomik olarak yazılır.
/// </summary>
public class DomainEventBufferTests : IntegrationTestBase
{
    public DomainEventBufferTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task AddDomainEvent_ThenSaveChanges_WritesOutboxRow()
    {
        // Arrange
        await using var db = CreateDbContext();
        var productId = Guid.NewGuid();
        var evt = new ProductAddedEvent(productId, "Test Ürün", Guid.NewGuid()) { TenantId = 1 };

        // Act
        db.AddDomainEvent(evt);
        await db.SaveChangesAsync();

        // Assert
        await using var readDb = CreateDbContext();
        var row = await readDb.NotificationOutbox
            .AsNoTracking()
            .SingleAsync(x => x.EventType == "ProductAddedEvent" && x.TenantId == 1);

        row.Status.Should().Be(OutboxStatus.Pending);
        row.TenantId.Should().Be(1);
        row.PayloadJson.Should().Contain("Test Ürün");
    }

    [Fact]
    public async Task AddDomainEvent_BufferClearedAfterSave()
    {
        // Arrange
        await using var db = CreateDbContext();
        var beforeCount = await db.NotificationOutbox.AsNoTracking().CountAsync();

        // Act — birinci SaveChangesAsync event'i yazar ve buffer'ı temizler
        db.AddDomainEvent(new ProductAddedEvent(Guid.NewGuid(), "A", Guid.NewGuid()) { TenantId = 1 });
        await db.SaveChangesAsync();

        // İkinci SaveChangesAsync buffer boş, ikinci satır eklenmemeli
        await db.SaveChangesAsync();

        // Assert
        await using var readDb = CreateDbContext();
        var afterCount = await readDb.NotificationOutbox.AsNoTracking().CountAsync();
        afterCount.Should().Be(beforeCount + 1, "buffer temizlenmiş olmalı, ikinci SaveChanges ek satır yazmamalı");
    }
}
