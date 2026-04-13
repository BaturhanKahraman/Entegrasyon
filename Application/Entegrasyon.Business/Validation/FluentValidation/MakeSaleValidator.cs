using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class MakeSaleValidator : AbstractValidator<MakeSaleDto>
    {
        public MakeSaleValidator()
        {
            RuleFor(x => x.SalePersonId).NotEmpty();
            RuleFor(x => x.BranchOfficeId).GreaterThan(0);
            RuleFor(x => x.SaleItems)
                .NotNull().WithMessage("Lütfen satılacak ürün gönderin.")
                .Must(x => x.Any()).WithMessage("En az bir ürün eklemelisiniz.");
            RuleFor(x => x.Payments)
                .NotNull().WithMessage("Ödeme bilgisi gereklidir.")
                .Must(x => x.Count > 0).WithMessage("En az bir ödeme yöntemi seçmelisiniz.");
            RuleForEach(x => x.Payments).ChildRules(p =>
            {
                p.RuleFor(x => x.PaymentMethodId).GreaterThan(0);
                p.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Ödeme tutarı sıfırdan büyük olmalıdır.");
            });
        }
    }
}
