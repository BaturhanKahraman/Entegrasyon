using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class MakeSaleValidator:AbstractValidator<MakeSaleDto>
    {
        public MakeSaleValidator()
        {
            RuleFor(x=>x.CustomerId).NotEmpty();
            RuleFor(x=>x.SalePersonId).NotEmpty();
            RuleFor(x => x.SaleItems)
                .NotNull().WithMessage("Lütfen satılacak ürün gönderin.")
                .Must(x=>x.Any());
        }
    }
}
