using System.ComponentModel.DataAnnotations;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Licenses;

public class LicenseListItemViewModel
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public LicenseType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }

    public string Status
    {
        get
        {
            var now = DateTime.UtcNow;
            if (now < StartDate) return "Yaklaşan";
            if (now > EndDate) return "Süresi Dolmuş";
            return "Aktif";
        }
    }

    public bool IsExpiringSoon =>
        Status == "Aktif" && (EndDate - DateTime.UtcNow).TotalDays <= 30;
}

public class LicenseListViewModel
{
    public List<LicenseListItemViewModel> ActiveLicenses { get; set; } = [];
    public List<LicenseListItemViewModel> ExpiredLicenses { get; set; } = [];
    public List<LicenseListItemViewModel> UpcomingLicenses { get; set; } = [];
}

public class LicenseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Firma seçimi zorunludur.")]
    [Display(Name = "Firma")]
    public int TenantId { get; set; }

    [Required(ErrorMessage = "Lisans tipi seçimi zorunludur.")]
    [Display(Name = "Lisans Tipi")]
    public LicenseType Type { get; set; } = LicenseType.Standard;

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddYears(1);

    [Display(Name = "Notlar")]
    public string? Notes { get; set; }

    // For dropdown
    public List<TenantDropdownItem> Tenants { get; set; } = [];
}

public class TenantDropdownItem
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
