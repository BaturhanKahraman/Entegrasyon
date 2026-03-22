namespace Entegrasyon.Entity.Dtos.Settings;

public sealed class UpdateApplicationSettingDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
