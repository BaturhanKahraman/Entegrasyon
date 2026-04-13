using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateSaleReturnValidator : AbstractValidator<CreateSaleReturnDto>
{
    public CreateSaleReturnValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.ReturnedByUserId).NotEmpty();
        RuleFor(x => x.ReturnReason).NotEmpty().WithMessage("İade nedeni zorunludur.");
        RuleFor(x => x.Items)
            .NotNull()
            .Must(x => x.Count > 0).WithMessage("En az bir kalem seçmelisiniz.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SaleItemId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("İade adedi sıfırdan büyük olmalıdır.");
        });
    }
}
