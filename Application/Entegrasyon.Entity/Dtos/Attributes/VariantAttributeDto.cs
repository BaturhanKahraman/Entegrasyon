namespace Entegrasyon.Entity.Dtos.Attributes;

public sealed record VariantAttributeDto(
int? CategoryAttributeValueId,
string CategoryAttributeValueName,
bool IsVarianter,
bool IsSlicer);