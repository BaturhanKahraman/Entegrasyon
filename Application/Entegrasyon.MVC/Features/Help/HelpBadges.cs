using Entegrasyon.Entity.Help;

namespace Entegrasyon.MVC.Features.Help;

/// <summary>
/// Yardım talebi kategori/durum rozetleri için tek kaynak (Index + Detail view'leri paylaşır).
/// Tabler badge light varyantı: "badge bg-&lt;renk&gt;-lt".
/// </summary>
public static class HelpBadges
{
    public static (string Css, string Text) Category(HelpRequestCategory c) => c switch
    {
        HelpRequestCategory.Bug => ("badge bg-red-lt", "Hata"),
        HelpRequestCategory.Suggestion => ("badge bg-blue-lt", "Öneri"),
        HelpRequestCategory.Question => ("badge bg-yellow-lt", "Soru"),
        _ => ("badge bg-secondary-lt", "Diğer")
    };

    public static (string Css, string Text) Status(HelpRequestStatus s) => s switch
    {
        HelpRequestStatus.Open => ("badge bg-orange-lt", "Açık"),
        HelpRequestStatus.Resolved => ("badge bg-green-lt", "Çözüldü"),
        _ => ("badge bg-secondary-lt", "-")
    };
}
