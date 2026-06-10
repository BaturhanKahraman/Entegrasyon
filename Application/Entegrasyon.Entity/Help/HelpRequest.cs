namespace Entegrasyon.Entity.Help;

/// <summary>
/// Kullanıcının yardım merkezinden gönderdiği destek/hata talebi.
/// Admin panelinden takip edilir ve çözüldü olarak işaretlenir.
/// </summary>
public sealed class HelpRequest : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Talebi gönderen kullanıcının Id'si (ApplicationUser.Id).</summary>
    public Guid UserId { get; set; }

    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
    public HelpRequestCategory Category { get; set; }
    public HelpRequestStatus Status { get; set; }
}

public enum HelpRequestCategory
{
    /// <summary>Hata raporu.</summary>
    Bug = 0,

    /// <summary>Öneri / geliştirme isteği.</summary>
    Suggestion = 1,

    /// <summary>Soru / genel destek.</summary>
    Question = 2
}

public enum HelpRequestStatus
{
    /// <summary>Açık — henüz çözülmedi.</summary>
    Open = 0,

    /// <summary>Çözüldü.</summary>
    Resolved = 1
}
