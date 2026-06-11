using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Dtos.Auth;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

/// <summary>
/// İlk-giriş şifre belirleme DTO doğrulaması. SADECE alan-düzeyi kurallar:
/// yeni şifre güçlü mü, onay eşleşiyor mu. Geçici şifre eşleşmesi (knowledge-proof)
/// burada DEĞİL — o bir business-rule olup AuthService içinde DB'ye karşı doğrulanır.
/// </summary>
public class SetInitialPasswordDtoValidator : AbstractValidator<SetInitialPasswordDto>
{
    public SetInitialPasswordDtoValidator()
    {
        RuleFor(x => x.TemporaryPassword)
            .NotEmpty().WithMessage(Messages.TemporaryPasswordRequired);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(Messages.NewPasswordRequired)
            .MinimumLength(8).WithMessage(Messages.PasswordTooShort)
            .Matches("[A-Za-z]").WithMessage(Messages.PasswordNeedsLetterAndDigit)
            .Matches("[0-9]").WithMessage(Messages.PasswordNeedsLetterAndDigit);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage(Messages.PasswordsDoNotMatch);
    }
}
