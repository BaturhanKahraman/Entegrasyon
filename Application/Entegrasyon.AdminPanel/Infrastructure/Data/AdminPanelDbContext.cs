using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.AdminPanel.Infrastructure.Data;

public class AdminPanelDbContext(DbContextOptions<AdminPanelDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantLicense> TenantLicenses => Set<TenantLicense>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();
    public DbSet<AiCreditAccount> AiCreditAccounts => Set<AiCreditAccount>();
    public DbSet<AiCreditTransaction> AiCreditTransactions => Set<AiCreditTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(t => t.Subdomain).IsUnique();
            e.HasMany(t => t.Licenses).WithOne(l => l.Tenant).HasForeignKey(l => l.TenantId);
            e.HasMany(t => t.ApplicationLogs).WithOne(l => l.Tenant).HasForeignKey(l => l.TenantId);
            e.HasOne(t => t.AiCreditAccount).WithOne(a => a.Tenant).HasForeignKey<AiCreditAccount>(a => a.TenantId);
        });

        modelBuilder.Entity<AiCreditAccount>(e =>
        {
            e.HasMany(a => a.Transactions).WithOne(t => t.Account).HasForeignKey(t => t.AiCreditAccountId);
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });
    }
}

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Tenant : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "PostgreSQL"; // PostgreSQL, SqlServer, etc.
    public bool IsActive { get; set; } = true;
    public int UserCount { get; set; }
    public string? Notes { get; set; }

    public ICollection<TenantLicense> Licenses { get; set; } = [];
    public ICollection<ApplicationLog> ApplicationLogs { get; set; } = [];
    public AiCreditAccount? AiCreditAccount { get; set; }
}

public class TenantLicense : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LicenseType Type { get; set; } = LicenseType.Standard;
    public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
    public string? Notes { get; set; }
}

public enum LicenseType
{
    Trial,
    Standard,
    Premium,
    Enterprise
}

public class AdminUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ApplicationLog : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Level { get; set; } = "Information"; // Information, Warning, Error, Critical
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? StackTrace { get; set; }
    public string? UserName { get; set; }
}

public class AiCreditAccount : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int ImageGenerationCredits { get; set; }
    public int ProductDescriptionCredits { get; set; }
    public ICollection<AiCreditTransaction> Transactions { get; set; } = [];
}

public class AiCreditTransaction : BaseEntity
{
    public int AiCreditAccountId { get; set; }
    public AiCreditAccount Account { get; set; } = null!;
    public AiCreditType CreditType { get; set; }
    public int Amount { get; set; } // Positive = add, Negative = consume
    public string? Description { get; set; }
}

public enum AiCreditType
{
    ImageGeneration,
    ProductDescription
}
