namespace Entegrasyon.Entity.Dtos.Category.AddStep;

public sealed record AddCategoryDtoStepOne(string Name,int? SuperCategoryId,bool IsFavorite);
public sealed record AddCategoryDtoStepTwo(int CategoryId,List<AddCategoryAttributeDto> AddCategoryAttributes);
