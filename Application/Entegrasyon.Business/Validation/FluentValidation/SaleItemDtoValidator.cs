using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class SaleItemDtoValidator : AbstractValidator<SaleItemDto>
{
    public SaleItemDtoValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue);

        RuleFor(x => x)
            .Must(x => !(x.DiscountPercent > 0 && x.DiscountAmount.HasValue && x.DiscountAmount.Value > 0))
            .WithMessage("İndirim yüzdesi ve TL indirimi aynı anda uygulanamaz — biri-ya-diğeri seçilmelidir.");

        RuleFor(x => x)
            .Must(x => !x.DiscountAmount.HasValue
                      || x.DiscountAmount.Value <= x.UnitPrice * x.Quantity)
            .WithMessage("TL indirimi satır toplamını aşamaz.");

        RuleFor(x => x.DiscountReasonNote)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.DiscountReasonNote));
    }
}
