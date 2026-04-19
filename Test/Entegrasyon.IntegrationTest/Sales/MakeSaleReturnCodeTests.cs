using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Sales;

[Trait("Category", "Integration")]
public class MakeSaleReturnCodeTests : IntegrationTestBase
{
    public MakeSaleReturnCodeTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task MakeSale_AssignsReturnCodeWithCorrectFormat()
    {
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var (_, variantId) = await SeedProductWithStockAsync(
            barcode: "RC-FORMAT-001", stock: 5, branchOfficeId: 1);

        int paymentMethodId;
        using (var db = CreateDbContext())
        {
            paymentMethodId = await db.PaymentMethodDefinitions
                .Where(p => p.IsActive).Select(p => p.Id).FirstAsync();
        }

        var dto = new MakeSaleDto(
            SalePersonId: userId,
            CustomerId: null,
            GeneralDiscount: 0m,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems:
            [
                new SaleItemDto(
                    ProductVariantId: variantId, TaxPercentage: 20, DiscountPercent: 0,
                    UnitPrice: 100m, Quantity: 1, DiscountVoucherCode: "", DiscountAmount: null)
            ],
            Payments:
            [
                new SalePaymentDto(PaymentMethodId: paymentMethodId, Amount: 120m,
                    CashReceived: 120m, CardAuthCode: null)
            ],
            GeneralDiscountReasonId: null,
            GeneralDiscountReasonNote: null);

        var (saleManager, scope) = GetScopedService<ISaleManager>();
        using var _ = scope;
        var result = await saleManager.MakeSale(dto);

        result.Success.Should().BeTrue(result.Message);

        using var verifyDb = CreateDbContext();
        var saved = await verifyDb.Sales.AsNoTracking().SingleAsync(s => s.Id == result.Data);

        saved.ReturnCode.Should().NotBeNull();
        saved.ReturnCode.Should().StartWith("R-");
        saved.ReturnCode!.Length.Should().Be(15);
    }

    [Fact]
    public async Task MakeSale_MultipleSales_ReturnCodesAreUnique()
    {
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var (_, variantId) = await SeedProductWithStockAsync(
            barcode: "RC-UNIQUE-001", stock: 50, branchOfficeId: 1);

        int paymentMethodId;
        using (var db = CreateDbContext())
        {
            paymentMethodId = await db.PaymentMethodDefinitions
                .Where(p => p.IsActive).Select(p => p.Id).FirstAsync();
        }

        var (saleManager, scope) = GetScopedService<ISaleManager>();
        using var _ = scope;

        var saleIds = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var dto = new MakeSaleDto(
                SalePersonId: userId,
                CustomerId: null,
                GeneralDiscount: 0m,
                BranchOfficeId: 1,
                SaleSource: SaleSource.POS,
                Note: null,
                SaleItems:
                [
                    new SaleItemDto(
                        ProductVariantId: variantId, TaxPercentage: 20, DiscountPercent: 0,
                        UnitPrice: 100m, Quantity: 1, DiscountVoucherCode: "", DiscountAmount: null)
                ],
                Payments:
                [
                    new SalePaymentDto(PaymentMethodId: paymentMethodId, Amount: 120m,
                        CashReceived: 120m, CardAuthCode: null)
                ],
                GeneralDiscountReasonId: null,
                GeneralDiscountReasonNote: null);

            var result = await saleManager.MakeSale(dto);
            result.Success.Should().BeTrue(result.Message);
            saleIds.Add(result.Data);
        }

        using var verifyDb = CreateDbContext();
        var codes = await verifyDb.Sales.AsNoTracking()
            .Where(s => saleIds.Contains(s.Id))
            .Select(s => s.ReturnCode)
            .ToListAsync();

        codes.Should().HaveCount(5);
        codes.Distinct().Should().HaveCount(5);
        codes.Should().AllSatisfy(c => c.Should().StartWith("R-"));
    }
}
