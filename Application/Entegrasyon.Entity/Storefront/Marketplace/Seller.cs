using Entegrasyon.Entity.Customers;

namespace Entegrasyon.Entity.Storefront;

public sealed class Seller : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Store info
    public string StoreName { get; set; } = null!;
    public string? StoreSlug { get; set; }
    public string? StoreDescription { get; set; }
    public string? LogoUrl { get; set; }

    // Company info
    public string CompanyName { get; set; } = null!;
    public string TaxNumber { get; set; } = null!;
    public string TaxOffice { get; set; } = null!;
    public string? Iban { get; set; }
    public string ContactPhone { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;

    // Status
    public SellerStatus Status { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Commission
    public decimal DefaultCommissionRate { get; set; } = 10;

    // Rating
    public double AverageRating { get; set; }
    public int TotalSales { get; set; }
}

public enum SellerStatus { Pending, Approved, Suspended, Rejected }
