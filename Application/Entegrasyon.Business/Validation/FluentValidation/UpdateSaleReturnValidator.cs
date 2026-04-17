using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class UpdateSaleReturnValidator : AbstractValidator<UpdateSaleReturnDto>
{
    public UpdateSaleReturnValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.UpdatedByUserId).NotEmpty();
        RuleFor(x => x.Source).IsInEnum();

        RuleFor(x => x.CustomReason)
            .NotEmpty()
            .When(x => !x.ReturnReasonId.HasValue)
            .WithMessage("İade nedeni veya özel açıklama zorunludur.");

        RuleFor(x => x.Items)
            .NotNull()
            .Must(x => x.Count > 0).WithMessage("En az bir kalem seçmelisiniz.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x).Must(x => x.SaleItemId.HasValue || x.OrderItemId.HasValue)
                .WithMessage("Kalem ID'si zorunludur.");
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}
