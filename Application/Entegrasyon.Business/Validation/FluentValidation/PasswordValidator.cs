using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Business.Validation.FluentValidation
{
    public class PasswordValidator:AbstractValidator<string>
    {
        public PasswordValidator()
        {
            RuleFor(x => x)
                .NotEmpty().WithMessage("Lütfen şifre alanını boş geçmeyin.")
                .Length(4,255).WithMessage("Lütfen 4 ve 255 karakter arasında bir değer girin.");
                

        }
    }
}
