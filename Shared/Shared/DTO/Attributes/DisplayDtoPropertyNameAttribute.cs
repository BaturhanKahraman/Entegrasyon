namespace Shared.DTO.Attributes;

[AttributeUsage(AttributeTargets.Property,AllowMultiple = false,Inherited = false)]
public class DisplayDtoPropertyNameAttribute : Attribute
{
    private string _displayName;
    public string DisplayName
    {
        get { return _displayName; }
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("DisplayName");
            if (value.Length >= 50)
                throw new ArgumentException("Gösterilen değer 50'den büyük olamaz.");
            _displayName = value;
        }
    }
    public DisplayDtoPropertyNameAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}
