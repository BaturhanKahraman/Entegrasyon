using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

[Trait("Category", "Integration")]
public class UserActivitySummaryIntegrationTests : IntegrationTestBase
{
    public UserActivitySummaryIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task<Guid> SeedUserAsync(bool isActive = true, DateTimeOffset? lastSeenAt = null)
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var shortKey = userId.ToString("N")[..12];
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            Name = "Aktivite",
            Surname = "Test",
            FullName = "Aktivite Test",
            Email = $"act-{shortKey}@test.com",
            UserName = $"act-{shortKey}",
            NormalizedUserName = $"ACT-{shortKey}",
            NormalizedEmail = $"ACT-{shortKey}@TEST.COM",
            IsActive = isActive,
            LastSeenAt = lastSeenAt,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task SeedProductLogAsync(Guid userId, LogAction action, int count)
    {
        using var db = CreateDbContext();
        for (var i = 0; i < count; i++)
        {
            db.Logs.Add(new ApplicationLog
            {
                ApplicationUserId = userId,
                LogType = LogType.Product,
                LogAction = action,
                Content = "Ürün işlemi",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedSaleAsync(Guid userId, int branchOfficeId = 1)
    {
        await SeedBasicEntitiesAsync(branchOfficeId: branchOfficeId);
        using var db = CreateDbContext();
        db.Sales.Add(new Sale
        {
            Id = Guid.NewGuid(),
            SalePersonId = userId,
            BranchOfficeId = branchOfficeId,
            SaleNumber = $"S-{Guid.NewGuid():N}".Substring(0, 12),
            SaleDate = DateTimeOffset.UtcNow,
            SaleSource = SaleSource.POS,
            SaleStatus = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserActivitySummary_ShouldAggregateProductAndSaleCounts()
    {
        var userId = await SeedUserAsync(lastSeenAt: DateTimeOffset.UtcNow.AddMinutes(-2));
        await SeedProductLogAsync(userId, LogAction.Add, 3);
        await SeedProductLogAsync(userId, LogAction.Update, 2);
        await SeedProductLogAsync(userId, LogAction.Delete, 1);
        await SeedSaleAsync(userId);
        await SeedSaleAsync(userId);

        var (manager, scope) = GetScopedService<IApplicationUserManager>();
        using var _ = scope;

        var result = await manager.GetUserActivitySummary(userId);

        result.Success.Should().BeTrue(result.Message);
        result.Data.ProductsAdded.Should().Be(3);
        result.Data.ProductsUpdated.Should().Be(2);
        result.Data.ProductsDeleted.Should().Be(1);
        result.Data.SalesCount.Should().Be(2);
        result.Data.IsOnline.Should().BeTrue();
        result.Data.LastSeenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserActivitySummary_OldLastSeen_ShouldBeOffline()
    {
        var userId = await SeedUserAsync(lastSeenAt: DateTimeOffset.UtcNow.AddMinutes(-30));

        var (manager, scope) = GetScopedService<IApplicationUserManager>();
        using var _ = scope;

        var result = await manager.GetUserActivitySummary(userId);

        result.Success.Should().BeTrue(result.Message);
        result.Data.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserActivitySummary_NullLastSeen_ShouldBeOffline_AndNotThrow()
    {
        // Geri-uyumluluk: yeni nullable LastSeenAt kolonu mevcut satırlarda NULL.
        // Online hesabı NULL'ı patlatmadan offline saymalı.
        var userId = await SeedUserAsync(lastSeenAt: null);

        var (manager, scope) = GetScopedService<IApplicationUserManager>();
        using var _ = scope;

        var result = await manager.GetUserActivitySummary(userId);

        result.Success.Should().BeTrue(result.Message);
        result.Data.IsOnline.Should().BeFalse();
        result.Data.LastSeenAt.Should().BeNull();
    }

    [Fact]
    public async Task GetUserActivitySummary_UnknownUser_ShouldReturnError()
    {
        var (manager, scope) = GetScopedService<IApplicationUserManager>();
        using var _ = scope;

        var result = await manager.GetUserActivitySummary(Guid.NewGuid());

        result.Success.Should().BeFalse();
    }
}
