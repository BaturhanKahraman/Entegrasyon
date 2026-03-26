namespace Entegrasyon.Entity.Dtos.Storefront;

public record FilterOptionDto(int AttributeId, string AttributeKey, string HumanizedKey, List<FilterValueDto> Values);
public record FilterValueDto(int ValueId, string DisplayText, int ProductCount);
public record BrandFilterDto(int BrandId, string BrandName, int ProductCount);
