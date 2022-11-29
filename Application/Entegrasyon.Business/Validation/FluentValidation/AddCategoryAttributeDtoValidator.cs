
using Entegrasyon.Entity.Dtos.Category;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCategoryAttributeDtoValidator:AbstractValidator<AddCategoryAttributeDto>
{
	public AddCategoryAttributeDtoValidator()
	{
		RuleFor(x => x.CategoryId).NotEmpty();
		RuleFor(x => x.AllowCustom)
			.NotEqual(false)
			.When(x => x.CategoryAttributeValues == null || x.CategoryAttributeValues.Count == 0);
		RuleFor(x => x.CategoryAttributeValues)
			.NotEmpty()
			.When(x=>x.AllowCustom==false);
	}
}
