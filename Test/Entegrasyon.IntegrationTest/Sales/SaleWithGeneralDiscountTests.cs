using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Sales;

[Trait("Category", "Integration")]
public class SaleWithGeneralDiscountTests : IntegrationTestBase
{
    public SaleWithGeneralDiscountTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task MakeSale_WithGeneralDiscount_PersistsAllFieldsAndCorrectTotals()
    {
        // Arrange — seed required entities
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();

        // Seed product + variant with stock (branchOfficeId=1, stock=10)
        var (_, variantId) = await SeedProductWithStockAsync(
            barcode: "DISC-TEST-001",
            stock: 10,
            branchOfficeId: 1);

        // PaymentMethodDefinitions are seeded by migration and preserved by Respawn.
        // Grab the first active one.
        int paymentMethodId;
        using (var db = CreateDbContext())
        {
            paymentMethodId = await db.PaymentMethodDefinitions
                .Where(p => p.IsActive)
                .Select(p => p.Id)
                .FirstAsync();
        }

        var dto = new MakeSaleDto(
            SalePersonId: userId,
            CustomerId: null,
            GeneralDiscount: 120m,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems:
            [
                new SaleItemDto(
                    ProductVariantId: variantId,
                    TaxPercentage: 20,
                    DiscountPercent: 0,
                    UnitPrice: 1000m,
                    Quantity: 1,
                    DiscountVoucherCode: "",
                    DiscountAmount: 100m)
            ],
            Payments:
            [
                new SalePaymentDto(
                    PaymentMethodId: paymentMethodId,
                    Amount: 980m,
                    CashReceived: 1000m,
                    CardAuthCode: null)
            ],
            GeneralDiscountReasonId: null,
            GeneralDiscountReasonNote: "Pazarlık");

        // Act
        var (saleManager, scope) = GetScopedService<ISaleManager>();
        using var _ = scope;
        var result = await saleManager.MakeSale(dto);

        // Assert — result is successful
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBe(Guid.Empty);

        // Assert — persisted fields
        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.Sales
            .AsNoTracking()
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == result.Data);

        saved.Should().NotBeNull();
        saved!.GeneralDiscount.Should().Be(120m);
        saved.GeneralDiscountReasonNote.Should().Be("Pazarlık");
        saved.GeneralDiscountReasonId.Should().BeNull();
        saved.SaleItems.Should().ContainSingle();
        saved.SaleItems.First().DiscountAmount.Should().Be(100m);
        saved.SaleStatus.Should().Be(SaleStatus.Completed);
    }
}
