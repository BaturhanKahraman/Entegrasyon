using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddUserDtoValidator : AbstractValidator<AddUserDto>
{
    public AddUserDtoValidator()
    {
        RuleFor(u => u.UserName)
            .MaximumLength(30).WithMessage("En fazla 30 karakter girebilirsiniz.")
            .NotEmpty().WithMessage("Boş olamaz.");
        RuleFor(u => u.Email)
            .EmailAddress().WithMessage("Lütfen bir e-posta girin")
            .MaximumLength(100).WithMessage("En fazla 100 karakter girebilirsiniz.");
        RuleFor(u => u.Name).MaximumLength(60).WithMessage("En fazla 60 karakter girebilirsiniz");
        RuleFor(u => u.Surname).MaximumLength(60).WithMessage("En fazla 60 karakter girebilirsiniz");

    }
}