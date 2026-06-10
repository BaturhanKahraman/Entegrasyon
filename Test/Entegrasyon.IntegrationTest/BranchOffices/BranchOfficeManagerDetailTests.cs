using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.BranchOffices;

/// <summary>
/// GetBranchDetailById gerçek EF Core PostgreSQL çevirisini test eder.
/// Regression: projeksiyon içinde b.Users.Count() + projeksiyon-sonrası FirstOrDefault(b => b.Id == ...)
/// EF Core'da "could not be translated" InvalidOperationException'ı fırlatıyordu (sayfa 500).
/// </summary>
[Trait("Category", "Integration")]
public class BranchOfficeManagerDetailTests : IntegrationTestBase
{
    public BranchOfficeManagerDetailTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task GetBranchDetailById_ShouldTranslateAndReturnDto_ForUserlessBranch()
    {
        // Arrange — kendi şubemizi seed et (sibling test class'lardan izole; HQ Id=1'e bağımlı değil)
        int branchId;
        using (var db = CreateDbContext())
        {
            var branch = new BranchOffice
            {
                Name = "Detay Boş Depo",
                NormalizedName = "DETAY BOS DEPO",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.BranchOffices.Add(branch);
            await db.SaveChangesAsync();
            branchId = branch.Id;
        }

        var (service, scope) = GetScopedService<IBranchOfficeManager>();
        using var _ = scope;

        // Act — regresyon: bu çağrı eskiden EF "could not be translated" ile patlıyordu
        var result = await service.GetBranchDetailById(branchId);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(branchId);
        result.Data.UserCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBranchDetailById_ShouldCountAssignedUsers()
    {
        // Arrange — yeni şube + bu şubeyi default yapan 2 kullanıcı
        int branchId;
        using (var db = CreateDbContext())
        {
            var branch = new BranchOffice
            {
                Name = "Detay Test Depo",
                NormalizedName = "DETAY TEST DEPO",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.BranchOffices.Add(branch);
            await db.SaveChangesAsync();
            branchId = branch.Id;

            var suffix = Guid.NewGuid().ToString("N")[..8];
            for (var i = 0; i < 2; i++)
            {
                var uid = Guid.NewGuid();
                var userName = $"d{suffix}{i}"; // varchar(30) sınırı içinde
                db.Users.Add(new ApplicationUser
                {
                    Id = uid,
                    Name = "User",
                    Surname = $"{i}",
                    FullName = $"User {i}",
                    Email = $"{userName}@test.com",
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    NormalizedEmail = $"{userName.ToUpperInvariant()}@TEST.COM",
                    IsActive = true,
                    DefaultBranchOfficeId = branchId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        var (service, scope) = GetScopedService<IBranchOfficeManager>();
        using var _ = scope;

        // Act
        var result = await service.GetBranchDetailById(branchId);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(branchId);
        result.Data.UserCount.Should().Be(2);
    }
}
