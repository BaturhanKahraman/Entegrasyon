using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.ViewModels.Customer;

public class CustomerDetailViewModel
{
    [Display(Name = "Müşteri Tipi")]
    public string? CustomerType { get; set; }
    [Display(Name = "Kayıt Tarihi")]
    public DateTimeOffset CreatedAt { get; set; }
    [Display(Name = "Id")]
    public int Id { get; init; }
    [Display(Name = "TC Kimlik/Vergi No")]
    public string? NationalIdentityOrTaxNumber { get; init; }
    [Display(Name = "İsim Soyisim")]
    public string? NameSurname { get; init; }
    [Display(Name = "Şirket İsmi")]
    public string? CorporateName { get; init; }
    [Display(Name = "Satış Adedi")]
    public int SalesCount { get; init; }
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Adres")]
    public string? Address { get; set; }
}
