
using Entegrasyon.Entity.Dtos.Category;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCategoryAttributeDtoValidator:AbstractValidator<AddCategoryAttributeDto>
{
	public AddCategoryAttributeDtoValidator()
	{
		RuleFor(x => x.CategoryAttributeValues)
			.NotEmpty();
	}
}
