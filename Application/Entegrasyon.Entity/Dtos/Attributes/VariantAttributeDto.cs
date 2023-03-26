namespace Entegrasyon.Entity.Dtos.Attributes;

public sealed record VariantAttributeDto(
int? CategoryAttributeValueId,
string CategoryAttributeValueName,
string CustomValue,
bool IsVarianter,
bool IsSlicer);