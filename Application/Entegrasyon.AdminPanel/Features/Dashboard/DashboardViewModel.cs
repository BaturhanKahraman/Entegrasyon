namespace Entegrasyon.AdminPanel.Features.Dashboard;

public class DashboardViewModel
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int ExpiringLicenses { get; set; }
    public int TotalLogs { get; set; }
    public List<RecentTenantViewModel> RecentTenants { get; set; } = [];
    public List<RecentLogViewModel> RecentLogs { get; set; } = [];
}

public class RecentTenantViewModel
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecentLogViewModel
{
    public int Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }
}
