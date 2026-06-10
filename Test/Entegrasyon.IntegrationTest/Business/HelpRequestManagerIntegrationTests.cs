using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Help;
using Entegrasyon.Entity.Help;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// HelpRequestManager integration testleri — gercek PostgreSQL ile yardim talebi akislari.
/// </summary>
[Trait("Category", "Integration")]
public class HelpRequestManagerIntegrationTests : IntegrationTestBase
{
    public HelpRequestManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task CreateAsync_ShouldPersistOpenRequest_WithTenantAndUser()
    {
        // Arrange
        var (manager, scope) = GetScopedService<IHelpRequestManager>();
        using var _ = scope;
        var userId = Guid.NewGuid();
        var dto = new CreateHelpRequestDto("Giris yapamiyorum", "Sifremi sifirladim ama olmadi.", HelpRequestCategory.Bug);

        // Act
        var result = await manager.CreateAsync(dto, userId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var saved = await dbContext.Set<HelpRequest>().FirstOrDefaultAsync(h => h.Subject == "Giris yapamiyorum");
        saved.Should().NotBeNull();
        saved!.Message.Should().Be("Sifremi sifirladim ama olmadi.");
        saved.Category.Should().Be(HelpRequestCategory.Bug);
        saved.Status.Should().Be(HelpRequestStatus.Open);
        saved.UserId.Should().Be(userId);
        saved.TenantId.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenSubjectEmpty()
    {
        // Arrange
        var (manager, scope) = GetScopedService<IHelpRequestManager>();
        using var _ = scope;
        var dto = new CreateHelpRequestDto("", "Mesaj dolu ama konu bos.", HelpRequestCategory.Question);

        // Act
        var act = async () => await manager.CreateAsync(dto, Guid.NewGuid());

        // Assert — pipeline 1. adim (FluentValidation) throw eder; MVC katmani ExceptionHandler ile yakalar.
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        using var dbContext = CreateDbContext();
        var any = await dbContext.Set<HelpRequest>().AnyAsync();
        any.Should().BeFalse("Validasyon basarisizsa kayit olusmamali");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnRequests_NewestFirst()
    {
        // Arrange
        var (manager, scope) = GetScopedService<IHelpRequestManager>();
        using var _ = scope;
        var userId = Guid.NewGuid();
        await manager.CreateAsync(new CreateHelpRequestDto("Ilk talep", "mesaj 1", HelpRequestCategory.Question), userId);
        await manager.CreateAsync(new CreateHelpRequestDto("Ikinci talep", "mesaj 2", HelpRequestCategory.Suggestion), userId);

        // Act
        var list = await manager.GetAllAsync();

        // Assert
        list.Should().HaveCount(2);
        list[0].Subject.Should().Be("Ikinci talep", "yeni → eski siralama");
        list[1].Subject.Should().Be("Ilk talep");
    }

    [Fact]
    public async Task MarkResolvedAsync_ShouldSetStatusResolved()
    {
        // Arrange
        var (manager, scope) = GetScopedService<IHelpRequestManager>();
        using var _ = scope;
        await manager.CreateAsync(new CreateHelpRequestDto("Cozulecek", "mesaj", HelpRequestCategory.Bug), Guid.NewGuid());

        int id;
        using (var db = CreateDbContext())
            id = await db.Set<HelpRequest>().Select(h => h.Id).FirstAsync();

        // Act
        var result = await manager.MarkResolvedAsync(id);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var saved = await dbContext.Set<HelpRequest>().FirstAsync(h => h.Id == id);
        saved.Status.Should().Be(HelpRequestStatus.Resolved);
    }

    [Fact]
    public async Task GetDetailAsync_ShouldReturnError_WhenNotFound()
    {
        // Arrange
        var (manager, scope) = GetScopedService<IHelpRequestManager>();
        using var _ = scope;

        // Act
        var result = await manager.GetDetailAsync(999999);

        // Assert
        result.Success.Should().BeFalse();
    }
}
