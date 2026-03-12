namespace Entegrasyon.Entity.Dtos.Brand;

/// <summary>
/// Dropdown/AutoComplete'te brand seçimi için kullanılır.
/// </summary>
public record BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
