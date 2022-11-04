using Entegrasyon.Entity.Dtos.DiscountVouchers;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateDiscountVoucherDtoValidator:AbstractValidator<CreateDiscountVoucherDto>
{
    public CreateDiscountVoucherDtoValidator()
    {
        RuleFor(x => x.Amount).NotEmpty().WithMessage("Lütfen indirim miktarını boş bırakmayın.");
        RuleFor(x => x.ExpiringDay)
            .GreaterThan(DateTimeOffset.Now)
            .When(x => x.ExpiringDay != null);
    }
}