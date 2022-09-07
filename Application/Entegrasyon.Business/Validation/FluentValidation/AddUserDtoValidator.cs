using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddUserDtoValidator:AbstractValidator<AddUserDto>
{
    public AddUserDtoValidator()
    {
        //TODO
        RuleFor(x => x.Name);
        RuleFor(x => x.Email);
        RuleFor(x => x.Surname);
        RuleFor(x => x.TemporaryPassword);
        RuleFor(x => x.UserName);
    }
}