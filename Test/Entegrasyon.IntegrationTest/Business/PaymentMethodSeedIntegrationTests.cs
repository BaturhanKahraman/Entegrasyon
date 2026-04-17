using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// SalesModuleSeedPaymentMethods ve DisableMealBankAndRenameCreditCard
/// migration'larının kombine sonucunu doğrular: yalnız Nakit, Kredi/Banka Kartı, Havale/EFT aktif.
/// </summary>
[Trait("Category", "Integration")]
public class PaymentMethodSeedIntegrationTests : IntegrationTestBase
{
    public PaymentMethodSeedIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task ActivePaymentMethods_AfterMigrations_ContainsOnlyExpectedSet()
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var activeCodes = await db.PaymentMethodDefinitions
            .Where(x => x.IsActive && x.TenantId == 1)
            .OrderBy(x => x.SortOrder)
            .Select(x => new { x.SystemCode, x.Name })
            .ToListAsync();

        activeCodes.Should().HaveCount(3);
        activeCodes.Select(x => x.SystemCode).Should().BeEquivalentTo(
            ["Cash", "CreditCard", "BankTransfer"]);

        var creditCard = activeCodes.Single(x => x.SystemCode == "CreditCard");
        creditCard.Name.Should().Be("Kredi/Banka Kartı");
    }

    [Fact]
    public async Task MealCardAndDebitCard_AfterMigration_AreInactive()
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        var disabled = await db.PaymentMethodDefinitions
            .Where(x => x.TenantId == 1 && new[] { "MealCard", "DebitCard" }.Contains(x.SystemCode))
            .ToListAsync();

        disabled.Should().HaveCount(2);
        disabled.Should().OnlyContain(x => !x.IsActive);
    }
}
