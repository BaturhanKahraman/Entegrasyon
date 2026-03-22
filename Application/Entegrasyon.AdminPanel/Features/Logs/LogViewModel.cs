namespace Entegrasyon.AdminPanel.Features.Logs;

public class LogListViewModel
{
    public List<LogItemViewModel> Logs { get; set; } = [];
    public LogFilterViewModel Filter { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<TenantSelectItem> Tenants { get; set; } = [];
}

public class LogItemViewModel
{
    public int Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LogFilterViewModel
{
    public int? TenantId { get; set; }
    public string? Level { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class LogDetailViewModel
{
    public int Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? StackTrace { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TenantSelectItem
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
