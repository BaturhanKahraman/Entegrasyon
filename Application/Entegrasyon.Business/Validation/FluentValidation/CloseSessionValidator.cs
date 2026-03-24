using Entegrasyon.Entity.Dtos.POS;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CloseSessionValidator : AbstractValidator<CloseSessionDto>
{
    public CloseSessionValidator()
    {
        RuleFor(x => x.SessionId)
            .GreaterThan(0).WithMessage("Oturum secimi zorunludur.");
        RuleFor(x => x.ClosingCash)
            .GreaterThanOrEqualTo(0).WithMessage("Kapanis kasasi sifir veya pozitif olmalidir.");
    }
}
