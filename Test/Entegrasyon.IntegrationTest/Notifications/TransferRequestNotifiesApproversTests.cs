using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Constants;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Notifications;

/// <summary>
/// Faz 7: Stok transfer talep iş akışındaki notification wiring'inin uçtan uca doğrulaması.
///
/// `StockTransferRequestManager.CreateAsync` → `Permissions.Stock.Transfer.Approve` claim sahiplerine bildirim
/// `ApproveAsync` → requester'a bildirim
/// `RejectAsync` → requester'a bildirim (gerekçeyle)
/// </summary>
public class TransferRequestNotifiesApproversTests : IntegrationTestBase
{
    private int _hqId;
    private int _sourceBranchId;
    private int _targetBranchId;
    private Guid _requesterId;
    private Guid _approverId;
    private Guid _variantId;

    public TransferRequestNotifiesApproversTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync();   // Brand + Category + (varsa) BranchOffice 1

        using var db = CreateDbContext();

        _hqId = await db.BranchOffices
            .Where(b => b.IsHeadquarters)
            .Select(b => b.Id)
            .FirstAsync();

        // Ayrı kaynak + hedef şubeler — HQ'yu kullanmıyoruz, transferler HQ değil HQ değil
        var src = new BranchOffice
        {
            Name = "Kaynak Depo",
            NormalizedName = "KAYNAK DEPO",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var tgt = new BranchOffice
        {
            Name = "Hedef Depo",
            NormalizedName = "HEDEF DEPO",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.BranchOffices.AddRange(src, tgt);
        await db.SaveChangesAsync();
        _sourceBranchId = src.Id;
        _targetBranchId = tgt.Id;

        // Source'da stoklu bir ürün
        var seeded = await SeedProductWithStockAsync(barcode: "TRF-001", stock: 100, branchOfficeId: _sourceBranchId);
        _variantId = seeded.VariantId;

        _requesterId = Guid.NewGuid();
        _approverId = Guid.NewGuid();

        db.Users.AddRange(
            new ApplicationUser
            {
                Id = _requesterId,
                UserName = "transfer-requester",
                NormalizedUserName = "TRANSFER-REQUESTER",
                FullName = "Transfer Talep Eden",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new ApplicationUser
            {
                Id = _approverId,
                UserName = "transfer-approver",
                NormalizedUserName = "TRANSFER-APPROVER",
                FullName = "Transfer Onaylayan",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

        db.Set<UsersClaims>().Add(new UsersClaims
        {
            ApplicationUserId = _approverId,
            Permission = PermissionConstants.StockTransferApprove
        });

        await db.SaveChangesAsync();
    }

    private List<TransferItemDto> BuildItems(int qty = 5) =>
        new() { new TransferItemDto(_variantId, qty) };

    [Fact]
    public async Task CreateTransfer_creates_notification_for_users_with_TransferApprove_permission()
    {
        // Act
        var manager = GetService<IStockTransferRequestManager>();
        var result = await manager.CreateAsync(_sourceBranchId, _targetBranchId, BuildItems(), _requesterId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var notif = await db.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Yeni stok transfer talebi")
            .SingleOrDefaultAsync();

        notif.Should().NotBeNull("create sonrası approver'a notification gitmeli");
        notif!.NotificationsUsers.Should().HaveCount(1);
        notif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_approverId);
        notif.ActionUrl.Should().StartWith("/stock-transfer-requests/");
        notif.Content.Should().Contain("Kaynak Depo");
        notif.Content.Should().Contain("Hedef Depo");
    }

    [Fact]
    public async Task ApproveTransfer_creates_notification_for_requester()
    {
        // Arrange
        var manager = GetService<IStockTransferRequestManager>();
        var createResult = await manager.CreateAsync(_sourceBranchId, _targetBranchId, BuildItems(), _requesterId);
        createResult.Success.Should().BeTrue(createResult.Message);
        var requestId = createResult.Data;

        // Act
        var approveResult = await manager.ApproveAsync(requestId, _approverId);

        // Assert
        approveResult.Success.Should().BeTrue(approveResult.Message);

        using var db = CreateDbContext();
        var requesterNotif = await db.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Transfer talebiniz onaylandı")
            .SingleOrDefaultAsync();

        requesterNotif.Should().NotBeNull("approve sonrası requester'a notification gitmeli");
        requesterNotif!.NotificationsUsers.Should().HaveCount(1);
        requesterNotif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_requesterId);
    }

    [Fact]
    public async Task RejectTransfer_creates_notification_for_requester_with_reason()
    {
        // Arrange
        var manager = GetService<IStockTransferRequestManager>();
        var createResult = await manager.CreateAsync(_sourceBranchId, _targetBranchId, BuildItems(), _requesterId);
        var requestId = createResult.Data;

        const string reason = "Kaynak şubede stok yeterli değil.";

        // Act
        var rejectResult = await manager.RejectAsync(requestId, _approverId, reason);

        // Assert
        rejectResult.Success.Should().BeTrue(rejectResult.Message);

        using var db = CreateDbContext();
        var rejectNotif = await db.Notifications
            .Include(n => n.NotificationsUsers)
            .Where(n => n.Header == "Transfer talebiniz reddedildi")
            .SingleOrDefaultAsync();

        rejectNotif.Should().NotBeNull("reject sonrası requester'a notification gitmeli");
        rejectNotif!.Content.Should().Contain(reason);
        rejectNotif.NotificationsUsers.Single().ApplicationUserId.Should().Be(_requesterId);
    }

    [Fact]
    public async Task CreateTransfer_with_no_approvers_does_not_throw()
    {
        // Arrange — approver claim'ini sil
        using (var db = CreateDbContext())
        {
            await db.Set<UsersClaims>()
                .Where(uc => uc.Permission == PermissionConstants.StockTransferApprove)
                .ExecuteDeleteAsync();
        }

        // Act
        var manager = GetService<IStockTransferRequestManager>();
        var result = await manager.CreateAsync(_sourceBranchId, _targetBranchId, BuildItems(), _requesterId);

        // Assert — talep yine de açılır, notification atlanır (recipients.Count == 0 guard)
        result.Success.Should().BeTrue("approver yok da olsa talep açılmalı");

        using var db2 = CreateDbContext();
        var anyNotif = await db2.Notifications
            .AnyAsync(n => n.Header == "Yeni stok transfer talebi");
        anyNotif.Should().BeFalse("recipient yoksa notification yaratılmamalı");
    }
}
