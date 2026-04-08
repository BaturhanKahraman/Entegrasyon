using Entegrasyon.Entity;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.BranchOffices;

/// <summary>
/// Faz 0 schema migration doğrulama testleri.
/// Filtered unique index, HQ seed row, yeni kolonların varlığı ve junction tablosunu doğrular.
/// Namespace "BranchOffices" (çoğul) — "BranchOffice" type ismiyle namespace shadowing önlenir.
/// </summary>
public class BranchOfficeSchemaTests : IntegrationTestBase
{
    public BranchOfficeSchemaTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    [Fact]
    public async Task Hq_seed_row_has_IsHeadquarters_true_and_normalized_name()
    {
        using var dbContext = CreateDbContext();

        var hq = await dbContext.BranchOffices
            .FirstOrDefaultAsync(b => b.Id == 1);

        hq.Should().NotBeNull();
        hq!.IsHeadquarters.Should().BeTrue();
        hq.NormalizedName.Should().Be("MERKEZ OFIS");
        hq.Name.Should().Be("Merkez Ofis");
    }

    [Fact]
    public async Task Duplicate_normalized_name_among_active_offices_throws_unique_violation()
    {
        using var dbContext = CreateDbContext();

        dbContext.BranchOffices.Add(new BranchOffice
        {
            Name = "İstanbul Deposu",
            NormalizedName = "ISTANBUL DEPOSU",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Aynı normalized name ile ikinci ofis — unique index ihlali
        using var dbContext2 = CreateDbContext();
        dbContext2.BranchOffices.Add(new BranchOffice
        {
            Name = "istanbul deposu",
            NormalizedName = "ISTANBUL DEPOSU",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var act = async () => await dbContext2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>(
            "filtered unique index, aktif ofisler arasında NormalizedName çakışmasını engellemeli");
    }

    [Fact]
    public async Task Soft_deleted_office_name_can_be_reused()
    {
        using var dbContext = CreateDbContext();

        // İlk ofisi oluştur
        var office = new BranchOffice
        {
            Name = "Ankara Deposu",
            NormalizedName = "ANKARA DEPOSU",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.BranchOffices.Add(office);
        await dbContext.SaveChangesAsync();

        // Soft-delete (filter dışında kalmalı)
        office.IsDeleted = true;
        office.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        // Aynı normalized name ile yeni (aktif) ofis — filtered unique index engellememeli
        using var dbContext2 = CreateDbContext();
        dbContext2.BranchOffices.Add(new BranchOffice
        {
            Name = "Ankara Deposu",
            NormalizedName = "ANKARA DEPOSU",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var act = async () => await dbContext2.SaveChangesAsync();
        await act.Should().NotThrowAsync(
            "filtered unique index sadece aktif (IsDeleted=false) ofisleri kontrol etmeli");

        // Doğrulama: artık iki ofis var, biri silinmiş biri aktif
        using var dbContext3 = CreateDbContext();
        var activeCount = await dbContext3.BranchOffices
            .Where(b => b.NormalizedName == "ANKARA DEPOSU")
            .CountAsync();
        activeCount.Should().Be(1, "query filter sadece aktif olanı gösterir");

        var totalIncludingDeleted = await dbContext3.BranchOffices
            .IgnoreQueryFilters()
            .Where(b => b.NormalizedName == "ANKARA DEPOSU")
            .CountAsync();
        totalIncludingDeleted.Should().Be(2, "silinmiş + yeni aktif = 2");
    }

    [Fact]
    public async Task BranchOffice_has_new_columns_and_navigations()
    {
        using var dbContext = CreateDbContext();

        var hq = await dbContext.BranchOffices.FirstAsync(b => b.Id == 1);

        hq.Address.Should().BeNull("HQ'nun adresi default null");
        hq.IsHeadquarters.Should().BeTrue();
        hq.DeletionRequestId.Should().BeNull("Pending silme talebi yok");
        hq.NormalizedName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UserBranchOffices_junction_table_exists_and_supports_composite_key()
    {
        using var dbContext = CreateDbContext();

        var adminUser = await dbContext.Users.FirstOrDefaultAsync();
        adminUser.Should().NotBeNull("seed kullanıcısı bulunmalı");

        var adminJunctions = await dbContext.UserBranchOffices
            .Where(x => x.UserId == adminUser!.Id)
            .ToListAsync();

        if (adminUser!.DefaultBranchOfficeId.HasValue)
        {
            adminJunctions.Should().NotBeEmpty(
                "migration backfill SQL mevcut atamaları junction'a taşımalı");
            adminJunctions.Should().Contain(j => j.BranchOfficeId == adminUser.DefaultBranchOfficeId.Value);
        }

        if (adminJunctions.Any())
        {
            var existing = adminJunctions.First();
            using var dbContext2 = CreateDbContext();
            dbContext2.UserBranchOffices.Add(new UserBranchOffice
            {
                UserId = existing.UserId,
                BranchOfficeId = existing.BranchOfficeId,
                AssignedAt = DateTimeOffset.UtcNow
            });

            var act = async () => await dbContext2.SaveChangesAsync();
            await act.Should().ThrowAsync<Exception>(
                "composite PK aynı (UserId, BranchOfficeId) çifti için duplicate insert'i engellemeli");
        }
    }

    [Fact]
    public async Task BranchOfficeDeletionRequests_table_exists_with_fk_to_requester()
    {
        using var dbContext = CreateDbContext();

        var user = await dbContext.Users.FirstAsync();

        var request = new BranchOfficeDeletionRequest
        {
            BranchOfficeId = 1,
            RequestedByUserId = user.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = BranchOfficeDeletionRequestStatus.Pending,
            HasStockTransfer = false
        };

        dbContext.BranchOfficeDeletionRequests.Add(request);
        await dbContext.SaveChangesAsync();

        request.Id.Should().BeGreaterThan(0);

        using var dbContext2 = CreateDbContext();
        var fetched = await dbContext2.BranchOfficeDeletionRequests
            .Include(r => r.BranchOffice)
            .Include(r => r.RequestedByUser)
            .FirstAsync(r => r.Id == request.Id);

        fetched.BranchOffice.Should().NotBeNull();
        fetched.BranchOffice.Id.Should().Be(1);
        fetched.RequestedByUser.Should().NotBeNull();
        fetched.Status.Should().Be(BranchOfficeDeletionRequestStatus.Pending);
    }

    [Fact]
    public async Task StockTransferRequests_table_exists_with_source_and_target_fks()
    {
        using var dbContext = CreateDbContext();

        var secondBranch = new BranchOffice
        {
            Name = "İzmir Deposu",
            NormalizedName = "IZMIR DEPOSU",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.BranchOffices.Add(secondBranch);
        await dbContext.SaveChangesAsync();

        var user = await dbContext.Users.FirstAsync();

        var request = new StockTransferRequest
        {
            SourceBranchOfficeId = 1,
            TargetBranchOfficeId = secondBranch.Id,
            RequestedByUserId = user.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = StockTransferRequestStatus.Pending,
            Items = new List<StockTransferRequestItem>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 5 }
            }
        };

        dbContext.StockTransferRequests.Add(request);
        await dbContext.SaveChangesAsync();

        request.Id.Should().BeGreaterThan(0);
        request.Items.Should().HaveCount(1);
        request.Items.First().Id.Should().BeGreaterThan(0, "child Items da insert edilmeli");
    }
}
