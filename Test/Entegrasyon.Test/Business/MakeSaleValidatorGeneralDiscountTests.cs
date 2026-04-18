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
}
