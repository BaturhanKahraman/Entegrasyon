namespace Entegrasyon.MVC.Features.Attributes.ViewModels;

public record AttributeValuesVm(int AttributeId, string AttributeName, List<AttributeValueRow> Values);

public record AttributeValueRow(int Id, string Name);
