using Entegrasyon.Entity.Dtos.Customers;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class UpdateCustomerDtoValidator:AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.Id).NotEqual(0).WithMessage("Lütfen müşteri seçiniz.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Lütfen müşteri adını boş bırakmayın.");
        RuleFor(x => x.Surname).NotEmpty().WithMessage("Lütfen müşteri soyadını boş bırakmayın.");
    }
}