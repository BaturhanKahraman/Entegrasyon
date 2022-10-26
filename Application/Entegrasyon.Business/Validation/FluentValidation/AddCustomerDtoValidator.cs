using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCustomerDtoValidator: AbstractValidator<AddCustomerDto>
{
    public AddCustomerDtoValidator()
    {
        RuleFor(x => x.NameOrCorporateName).NotEmpty().WithMessage("Lütfen müşteri/şirket adını boş bırakmayın.");
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Lütfen müşteri tipini boş bırakmayın.");
        
    }
}