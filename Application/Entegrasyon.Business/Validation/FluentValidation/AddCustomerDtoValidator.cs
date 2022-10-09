using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCustomerDtoValidator: AbstractValidator<AddCustomerDto>
{
    public AddCustomerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Lütfen müşteri adını boş bırakmayın.");
        RuleFor(x => x.Surname).NotEmpty().WithMessage("Lütfen müşteri soyadını boş bırakmayın.");
    }
}