using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryAttributeDto(
int Id,
bool IsRequired,
bool IsVarianter,
bool IsSlicer,
DateTimeOffset CreatedAt,
string CategoryAttributeKey,
string CategoriyAttributeHumanized,
List<CategoryAttributeValue> CategoryAttributeValues);