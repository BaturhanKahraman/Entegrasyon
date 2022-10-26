using Entegrasyon.Entity.Dtos.Customers;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class UpdateCustomerDtoValidator:AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.Id).NotEqual(0).WithMessage("Lütfen müşteri seçiniz.");
    }
}