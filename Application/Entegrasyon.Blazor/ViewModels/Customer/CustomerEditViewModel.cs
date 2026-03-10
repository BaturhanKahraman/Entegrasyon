using System.ComponentModel.DataAnnotations;
using Entegrasyon.Blazor.Attributes;

namespace Entegrasyon.Blazor.ViewModels.Customer;

public class CustomerEditViewModel
{
    public int Id { get; set; }

    [DataType(DataType.PhoneNumber)]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Müşteri Tipi")]
    [Required(ErrorMessage = "Lütfen müşteri tipini girin!")]
    public string? CustomerType { get; set; }
    [Display(Name = "İsim")]
    public string? Name { get; set; }
    [Display(Name = "Soyisim")]
    public string? Surname { get; set; }
    [Display(Name = "TC Kimlik No")]
    [MaxLength(11,ErrorMessage = "En çok 11 karakter girebilirsiniz.")]
    public string? NationalIdentity { get; set; }
    [Display(Name = "Vergi Numarası")]
    [RequiredIf(nameof(CustomerType),ComparisonType.EqualTo,"Corporate",ErrorMessage = "Kurumsal müşteride vergi no girmek zorunludur.")]
    public string? TaxNumber { get; set; }
    [Display(Name = "Şirket")]
    [RequiredIf(nameof(CustomerType),ComparisonType.EqualTo,"Corporate",ErrorMessage = "Kurumsal müşteride vergi no girmek zorunludur.")]
    public string? CorporateName { get; set; }
}
