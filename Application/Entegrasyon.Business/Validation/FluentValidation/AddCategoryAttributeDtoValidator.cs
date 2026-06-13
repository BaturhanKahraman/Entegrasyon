using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCategoryAttributeDtoValidator:AbstractValidator<AddCategoryAttributeDto>
{
	public AddCategoryAttributeDtoValidator()
	{
		RuleFor(x => x.CategoryAttributeKey)
			.NotEmpty().WithMessage("Anahtar boş olamaz.")
			.MaximumLength(255).WithMessage("Anahtar en fazla 255 karakter olabilir.");
		RuleFor(x => x.CategoryAttributeHumanized)
			.NotEmpty().WithMessage("Görünen ad boş olamaz.")
			.MaximumLength(255).WithMessage("Görünen ad en fazla 255 karakter olabilir.");
		RuleFor(x => x.CategoryAttributeValues)
			.NotEmpty().WithMessage("En az bir değer tanımlanmalıdır.");
		RuleForEach(x => x.CategoryAttributeValues).ChildRules(value =>
		{
			value.RuleFor(v => v.Name)
				.NotEmpty().WithMessage("Değer adı boş olamaz.")
				.MaximumLength(CategoryAttributeValue.MaxNameLength)
				.WithMessage($"Değer adı en fazla {CategoryAttributeValue.MaxNameLength} karakter olabilir.");
		});
	}
}
