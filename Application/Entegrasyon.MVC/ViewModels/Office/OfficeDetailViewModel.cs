using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.ViewModels.Office;

public class OfficeDetailViewModel
{
    [Display(Name = "Kimlik Numarası")]
    public int Id { get; set; }
    [Display(Name = "Ofis İsmi")]
    public string Name { get; set; }
    [Display(Name = "Kullanıcı Sayısı")]
    public int UserCount { get; set; }
    [Display(Name = "Oluşturulma Tarihi")]
    public DateTimeOffset CreatedAt{ get; set; }
}