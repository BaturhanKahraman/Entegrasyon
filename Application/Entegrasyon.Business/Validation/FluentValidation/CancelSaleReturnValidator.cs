using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CancelSaleReturnValidator : AbstractValidator<CancelSaleReturnDto>
{
    public CancelSaleReturnValidator()
    {
        RuleFor(x => x.ReturnId).GreaterThan(0);
        RuleFor(x => x.CancelledByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(5)
            .WithMessage("İptal nedeni en az 5 karakter olmalıdır.");
    }
}
