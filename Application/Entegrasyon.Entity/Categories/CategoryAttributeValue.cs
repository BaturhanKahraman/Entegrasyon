using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Categories;

public class CategoryAttributeValue : BaseEntity
{
    /// <summary>Değer adı için izin verilen maksimum uzunluk. NormalizedName kolonu varchar(256);
    /// normalize (upper) çok-baytlıda büyüyebileceği için 256 altında headroom. Tek doğruluk kaynağı.</summary>
    public const int MaxNameLength = 200;

    public int Id { get; set; }
    public string? Name { get; set; }
    public string NormalizedName { get; set; } = string.Empty;
    public int CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;
}