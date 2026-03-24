using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Templates;

namespace Entegrasyon.AdminPanel.Features.MatchedEntities;

public class PackageListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MatchedEntityType EntityType { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; }
    public int EntityCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string EntityTypeDisplay => EntityType switch
    {
        MatchedEntityType.Category => "Kategori",
        MatchedEntityType.Brand => "Marka",
        MatchedEntityType.CargoCompany => "Kargo Şirketi",
        _ => EntityType.ToString()
    };
}

public class PackageFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Paket adı zorunludur.")]
    [MaxLength(500, ErrorMessage = "Paket adı en fazla 500 karakter olabilir.")]
    [Display(Name = "Paket Adı")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Entity türü zorunludur.")]
    [Display(Name = "Entity Türü")]
    public MatchedEntityType EntityType { get; set; }

    [Display(Name = "Yayınlandı")]
    public bool IsPublished { get; set; }
}

public class PackageDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MatchedEntityType EntityType { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<TemplateCategoryViewModel> Categories { get; set; } = [];
    public List<TemplateBrandViewModel> Brands { get; set; } = [];
    public List<TemplateCargoCompanyViewModel> CargoCompanies { get; set; } = [];
}

public class TemplateCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? DefaultVatRate { get; set; }
    public int AttributeCount { get; set; }
    public int MarketplaceCount { get; set; }
    public List<TemplateCategoryViewModel> Children { get; set; } = [];
}

public class TemplateBrandViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MarketplaceCount { get; set; }
}

public class TemplateCargoCompanyViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int MarketplaceCount { get; set; }
}
