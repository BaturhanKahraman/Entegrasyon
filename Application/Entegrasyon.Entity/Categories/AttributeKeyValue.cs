using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Categories;

public class AttributeKeyValue : ApplicationEntity
{
    public int CategoryAttributeKey { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; }
    public int CategoryAttributeValue { get; set; }
    public CategoryAttributeValue AttributeValue { get; set; }

}