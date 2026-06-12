using Entegrasyon.Entity.Dtos.Reports;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class SendRecoveryCouponDtoValidator : AbstractValidator<SendRecoveryCouponDto>
{
    public SendRecoveryCouponDtoValidator()
    {
        RuleFor(x => x.VoucherId)
            .GreaterThan(0).WithMessage("Lütfen gönderilecek bir indirim kuponu seçin.");

        RuleFor(x => x.CustomerIds)
            .NotNull().WithMessage("Lütfen en az bir müşteri seçin.")
            .Must(ids => ids is { Count: > 0 }).WithMessage("Lütfen en az bir müşteri seçin.");
    }
}
