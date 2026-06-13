using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Features.Attributes.ViewModels;

/// <summary>Yeni özellik oluşturma formu — Key = Humanized = Name (anahtar sonradan değiştirilemez).</summary>
public class CreateAttributeVm
{
    [Required(ErrorMessage = "Özellik adı zorunludur.")]
    [MaxLength(255, ErrorMessage = "Özellik adı en fazla 255 karakter olabilir.")]
    [Display(Name = "Özellik Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "En az bir değer girilmelidir.")]
    [Display(Name = "Değerler")]
    public string Values { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
