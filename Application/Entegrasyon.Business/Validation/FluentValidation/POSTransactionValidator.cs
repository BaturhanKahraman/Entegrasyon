using Entegrasyon.Entity.Dtos.POS;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class POSTransactionValidator : AbstractValidator<POSTransactionDto>
{
    public POSTransactionValidator()
    {
        RuleFor(x => x.POSSessionId)
            .GreaterThan(0).WithMessage("Oturum secimi zorunludur.");
        RuleFor(x => x.Sale)
            .NotNull().WithMessage("Satis bilgisi zorunludur.");
        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Gecerli bir odeme yontemi seciniz.");
    }
}
