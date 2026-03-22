using System.ComponentModel.DataAnnotations;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Tenants;

public class TenantListViewModel
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TenantDetailViewModel
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Summary counts
    public int ActiveLicenseCount { get; set; }
    public int TotalLicenseCount { get; set; }
    public string? CurrentLicenseType { get; set; }
    public int LogCount { get; set; }
    public int ErrorLogCount { get; set; }
    public int ImageCredits { get; set; }
    public int DescriptionCredits { get; set; }
}

public class TenantFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Firma adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "Firma adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Firma Adı")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subdomain zorunludur.")]
    [MaxLength(100, ErrorMessage = "Subdomain en fazla 100 karakter olabilir.")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Subdomain sadece küçük harf, rakam ve tire içerebilir.")]
    [Display(Name = "Subdomain")]
    public string Subdomain { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string? ContactEmail { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon")]
    public string? ContactPhone { get; set; }

    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [MaxLength(20, ErrorMessage = "Vergi numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "Vergi Numarası")]
    public string? TaxNumber { get; set; }

    [Required(ErrorMessage = "Bağlantı dizesi zorunludur.")]
    [Display(Name = "Bağlantı Dizesi")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "Veritabanı tipi zorunludur.")]
    [Display(Name = "Veritabanı Tipi")]
    public string DatabaseType { get; set; } = "PostgreSQL";

    [Display(Name = "Notlar")]
    public string? Notes { get; set; }

    public static readonly string[] DatabaseTypes = ["PostgreSQL", "SqlServer", "MySql", "Sqlite"];
}
