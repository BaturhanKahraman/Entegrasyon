using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class MakeSaleValidatorGeneralDiscountTests
{
    private readonly MakeSaleValidator _sut = new();

    private static MakeSaleDto Valid(decimal generalDiscount)
        => new(
            SalePersonId: Guid.NewGuid(),
            CustomerId: null,
            GeneralDiscount: generalDiscount,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: [new SaleItemDto(
                ProductVariantId: Guid.NewGuid(),
                TaxPercentage: 20,
                DiscountPercent: 0,
                UnitPrice: 100m,
                Quantity: 1,
                DiscountVoucherCode: "")],
            Payments: [new SalePaymentDto(PaymentMethodId: 1, Amount: 100m, null, null)]);

    [Fact]
    public void Validator_RejectsNegativeGeneralDiscount()
    {
        var dto = Valid(-1m);
        var result = _sut.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_RejectsGeneralDiscountExceedingSubtotalGross()
    {
        // Subtotal gross = 100 * 1.20 = 120
        var dto = Valid(500m);
        var result = _sut.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_AcceptsZeroGeneralDiscount()
    {
        var dto = Valid(0m);
        var result = _sut.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_AcceptsDiscountWithinSubtotal()
    {
        var dto = Valid(50m);
        var result = _sut.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_CountsLineDiscount_InSubtotal()
    {
        // Kalem: UnitPrice 100, Qty 1, line discount 50, KDV %20
        // net=50, gross=60 → GeneralDiscount 70 reddedilmeli
        var dto = new MakeSaleDto(
            SalePersonId: Guid.NewGuid(),
            CustomerId: null,
            GeneralDiscount: 70m,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: [new SaleItemDto(
                ProductVariantId: Guid.NewGuid(),
                TaxPercentage: 20,
                DiscountPercent: 0,
                UnitPrice: 100m,
                Quantity: 1,
                DiscountVoucherCode: "",
                DiscountAmount: 50m)],   // per-line discount
            Payments: [new SalePaymentDto(PaymentMethodId: 1, Amount: 50m, null, null)]);

        var result = _sut.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.GeneralDiscount);
    }

    [Fact]
    public void Validator_HandlesNullSaleItems_WithoutThrowing()
    {
        var dto = new MakeSaleDto(
            SalePersonId: Guid.NewGuid(),
            CustomerId: null,
            GeneralDiscount: 50m,
            BranchOfficeId: 1,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: null!,
            Payments: []);

        // Beklenen: NRE atmaz, cross-field kural geçer (SaleItems.NotNull farklı rule ile yakalanır)
        var act = () => _sut.TestValidate(dto);
        act.Should().NotThrow<NullReferenceException>();
    }
}
