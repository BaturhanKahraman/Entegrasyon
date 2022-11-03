using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCustomerDtoValidator: AbstractValidator<AddCustomerDto>
{
    public AddCustomerDtoValidator()
    {
        RuleFor(x => x.CustomerType)
            .NotEmpty().WithMessage("Lütfen müşteri tipini boş bırakmayın.");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Lütfen müşteri adını boş bırakmayın.")
            .When(x=>x.CustomerType=="Retail");
        RuleFor(x => x.CorporateName)
            .NotEmpty()
            .WithMessage("Lütfen şirket adını boş bırakmayın.")
            .When(x=>x.CustomerType!="Retail");
    }
}