using Entegrasyon.Entity.Dtos.POS;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCashMovementValidator : AbstractValidator<AddCashMovementDto>
{
    public AddCashMovementValidator()
    {
        RuleFor(x => x.POSSessionId)
            .GreaterThan(0).WithMessage("Oturum secimi zorunludur.");
        RuleFor(x => x.MovementType)
            .IsInEnum().WithMessage("Gecerli bir hareket tipi seciniz.");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Tutar sifirdan buyuk olmalidir.");
    }
}
