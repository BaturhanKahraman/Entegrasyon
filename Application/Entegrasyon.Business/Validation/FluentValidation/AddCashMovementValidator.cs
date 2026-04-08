using Entegrasyon.Entity.Dtos.POS;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCashMovementValidator : AbstractValidator<AddCashMovementDto>
{
    public AddCashMovementValidator()
    {
        RuleFor(x => x.POSSessionId)
            .GreaterThan(0).WithMessage("Oturum seçimi zorunludur.");
        RuleFor(x => x.MovementType)
            .IsInEnum().WithMessage("Geçerli bir hareket tipi seçiniz.");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Tutar sıfırdan büyük olmalıdır.");
    }
}
