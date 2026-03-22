using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.AdminPanel.Features.AiCredits;

public class AiCreditListViewModel
{
    public List<AiCreditListItemViewModel> Items { get; set; } = [];
}

public class AiCreditListItemViewModel
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ImageGenerationCredits { get; set; }
    public int ProductDescriptionCredits { get; set; }
    public bool HasAccount { get; set; }
}

public class AiCreditDetailViewModel
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ImageGenerationCredits { get; set; }
    public int ProductDescriptionCredits { get; set; }
    public List<AiCreditTransactionViewModel> Transactions { get; set; } = [];
}

public class AiCreditTransactionViewModel
{
    public int Id { get; set; }
    public string CreditType { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddCreditsFormViewModel
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kredi tipi secimi zorunludur.")]
    [Display(Name = "Kredi Tipi")]
    public string CreditType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Miktar zorunludur.")]
    [Range(1, 100000, ErrorMessage = "Miktar 1 ile 100.000 arasinda olmalidir.")]
    [Display(Name = "Miktar")]
    public int Amount { get; set; }

    [Display(Name = "Aciklama")]
    [MaxLength(500, ErrorMessage = "Aciklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
}
