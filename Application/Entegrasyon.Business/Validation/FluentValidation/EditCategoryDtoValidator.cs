using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Users;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditCategoryDtoValidator : AbstractValidator<EditCategoryDto>
{
    public EditCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}