using System.ComponentModel;

namespace Entegrasyon.Business.Extensions;

[AttributeUsage(AttributeTargets.Property)]
public class EntityNameAttribute:Attribute
{
    private readonly string _entityPropertyName;

    public EntityNameAttribute()
    {
        var propertyType = TypeDescriptor.GetAttributes(this)[0].GetType();
        string name = propertyType.Name;
        _entityPropertyName = name;
    }
    public EntityNameAttribute(string entityPropertyName)
    {
        _entityPropertyName = entityPropertyName;
    }
}
