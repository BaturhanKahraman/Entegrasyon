namespace Entegrasyon.Entity.Dtos.Attributes;

public sealed record AttributeKeyValueDto(
int CategoryAttributeId,
string CategoryAttributeName,
int? AttributeValueId,
string AttributeValueName,
bool IsRequired,
string CustomValue);