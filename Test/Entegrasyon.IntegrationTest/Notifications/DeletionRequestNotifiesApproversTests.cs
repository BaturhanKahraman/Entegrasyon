using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Notifications;

/// <summary>
/// Faz 7: Şube ofisi silme talep iş akışındaki notification wiring'inin uçtan uca doğrulaması.
///
/// Test stratejisi: Manager metodlarının try/catch içine sarılı `notificationManager.SendNotification`
/// çağrıları, gerçek `NotificationManager` üzerinden DB'deki `Notifications` + `NotificationsUsers`
/// satırlarını yaratmalı. Test bu satırları assert eder.
///
/// Önkoşullar:
///  - Talep açan kullanıcı (requester) — sadece basit kullanıcı
///  - Onay verebilen kullanıcı (approver) — UsersClaims tablosunda
///    `Permissions.StockOffice.Delete.Approve` permission'ı ile
/// </summary>
public class DeletionRequestNotifiesApproversTests : IntegrationTestBase
{
    private int _hqId;
    private int _branchToDeleteId;
    private Guid _requesterId;
    private Guid _approverId;

    public DeletionRequestNotifiesApproversTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();

        _hqId = await db.BranchOffices
            .Where(b => b.IsHeadquarters)
            .Select(b => b.Id)
            .FirstAsync();

        // Silinecek şube — boş (stok yok), iş kuralları temiz geçsin
        var branch = new BranchOffice
        {
            Name = "Silinecek Şube",
            NormalizedName = "SILINECEK SUBE",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.BranchOffices.Add(branch);
        await db.SaveChangesAsync();
        _branchToDeleteId = branch.Id;

        _requesterId = Guid.NewGuid();
        _approverId = Guid.NewGuid();

        db.Users.AddRange(
            new ApplicationUser
            {
                Id = _requesterId,
                UserName = "deletion-requester",
                NormalizedUserName = "DELETION-REQUESTER",
                FullName = "Talep Açan",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new ApplicationUser
            {
                Id = _approverId,
                UserName = "deletion-approver",
                NormalizedUserName = "DELETION-APPROVER",
                FullName = "Onay Veren",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

        // Approver'a doğrudan claim ata — rol bypass'lı
        db.Set<UsersClaims>().Add(new UsersClaims
        {
            ApplicationUserId = _approverId,
            Permission = PermissionConstants.StockOfficeDeleteApprove
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task RequestDelete_creates_notification_for_users_with_DeleteApprove_permission()
    {
        // Act
        var manager = GetService<IBranchOfficeDeletionRequestManager>();
        var result = await manager.RequestDeleteAsync(_branchToDeleteId, _requesterId, targetBranchOfficeId: null);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var notifications = await db.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Şube silme talebi")
            .ToListAsync();

        notifications.Should().HaveCount(1, "tek talep → tek notification");
        var notif = notifications.Single();
        notif.NotificationsUsers.Should().HaveCount(1, "yalnızca approver claim sahibi recipient'tır");
        notif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_approverId);
        notif.ActionUrl.Should().StartWith("/branch-office-deletion-requests/");
    }

    [Fact]
    public async Task ApproveDelete_creates_notification_for_requester()
    {
        // Arrange — önce talep aç
        var manager = GetService<IBranchOfficeDeletionRequestManager>();
        var requestResult = await manager.RequestDeleteAsync(_branchToDeleteId, _requesterId, targetBranchOfficeId: null);
        requestResult.Success.Should().BeTrue(requestResult.Message);
        var requestId = requestResult.Data;

        // Act — onayla
        var approveResult = await manager.ApproveAsync(requestId, _approverId);

        // Assert
        approveResult.Success.Should().BeTrue(approveResult.Message);

        using var db = CreateDbContext();
        var requesterNotif = await db.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Silme talebi onaylandı")
            .SingleOrDefaultAsync();

        requesterNotif.Should().NotBeNull("approve sonrası requester'a notification gitmeli");
        requesterNotif!.NotificationsUsers.Should().HaveCount(1);
        requesterNotif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_requesterId);
    }

    [Fact]
    public async Task RejectDelete_creates_notification_for_requester_with_reason()
    {
        // Arrange
        var manager = GetService<IBranchOfficeDeletionRequestManager>();
        var requestResult = await manager.RequestDeleteAsync(_branchToDeleteId, _requesterId, targetBranchOfficeId: null);
        var requestId = requestResult.Data;

        const string reason = "Bu şube hâlâ aktif kullanılıyor.";

        // Act
        var rejectResult = await manager.RejectAsync(requestId, _approverId, reason);

        // Assert
        rejectResult.Success.Should().BeTrue(rejectResult.Message);

        using var db = CreateDbContext();
        var rejectNotif = await db.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Silme talebi reddedildi")
            .SingleOrDefaultAsync();

        rejectNotif.Should().NotBeNull("reject sonrası requester'a notification gitmeli");
        rejectNotif!.Content.Should().Contain(reason, "gerekçe content içinde olmalı");
        rejectNotif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_requesterId);
    }

    [Fact]
    public async Task RequestDelete_does_not_notify_inactive_users_with_permission()
    {
        // Arrange — pasif bir approver daha ekle
        var inactiveApproverId = Guid.NewGuid();
        using (var db = CreateDbContext())
        {
            db.Users.Add(new ApplicationUser
            {
                Id = inactiveApproverId,
                UserName = "inactive-approver",
                NormalizedUserName = "INACTIVE-APPROVER",
                IsActive = false,                           // ← KRİTİK
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.Set<UsersClaims>().Add(new UsersClaims
            {
                ApplicationUserId = inactiveApproverId,
                Permission = PermissionConstants.StockOfficeDeleteApprove
            });
            await db.SaveChangesAsync();
        }

        // Act
        var manager = GetService<IBranchOfficeDeletionRequestManager>();
        await manager.RequestDeleteAsync(_branchToDeleteId, _requesterId, targetBranchOfficeId: null);

        // Assert
        using var db2 = CreateDbContext();
        var notif = await db2.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Şube silme talebi")
            .SingleAsync();

        notif.NotificationsUsers.Should().HaveCount(1, "yalnızca aktif approver bildirilmeli");
        notif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_approverId);
    }
}
