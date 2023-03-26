using System.ComponentModel;

namespace Shared.Expressions;

[AttributeUsage(AttributeTargets.Property)]
public class EntityNameAttribute:Attribute
{
    private readonly string EntityPropertyName;

    public EntityNameAttribute()
    {
        var propertyType = TypeDescriptor.GetAttributes(this)[0].GetType();
        string name = propertyType.Name;
        EntityPropertyName = name;
    }
    public EntityNameAttribute(string entityPropertyName)
    {
        EntityPropertyName = entityPropertyName;
    }
}