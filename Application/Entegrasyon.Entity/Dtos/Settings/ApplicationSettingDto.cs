using Entegrasyon.Entity.Settings;

namespace Entegrasyon.Entity.Dtos.Settings;

public sealed class ApplicationSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Group { get; set; }
    public SettingValueType ValueType { get; set; }
}
