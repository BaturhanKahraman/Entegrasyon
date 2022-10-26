using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class SaleValidator:AbstractValidator<MakeSaleDto>
    {
        public SaleValidator()
        {
            RuleFor(x=>x.CustomerId).NotEmpty();
            RuleFor(x=>x.SalePersonId).NotEmpty();
            RuleFor(x => x.SaleItems)
                .NotNull().WithMessage("Lütfen satılacak ürün gönderin.")
                .Must(x=>x.Any());
        }
    }
}
