using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductActivityTimeline : ComponentBase
{
    [Parameter, EditorRequired] public Guid ProductId { get; set; }
    [Inject] private IProductActivityLogger ActivityLogger { get; set; } = null!;

    private List<ProductActivityLog> _activities = [];
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        var result = await ActivityLogger.GetTimelineAsync(ProductId);
        if (result.Success)
            _activities = result.Data;
        _loading = false;
    }

    private static Color GetColor(ProductActivityStatus status) => status switch
    {
        ProductActivityStatus.Success => Color.Success,
        ProductActivityStatus.Warning => Color.Warning,
        ProductActivityStatus.Error => Color.Error,
        _ => Color.Info
    };

    private static Severity GetSeverity(ProductActivityStatus status) => status switch
    {
        ProductActivityStatus.Success => Severity.Success,
        ProductActivityStatus.Warning => Severity.Warning,
        ProductActivityStatus.Error => Severity.Error,
        _ => Severity.Info
    };

    private static string GetActivityLabel(ProductActivityType type) => type switch
    {
        ProductActivityType.Created => "Oluşturuldu",
        ProductActivityType.Updated => "Güncellendi",
        ProductActivityType.MappingValidated => "Eşleştirme Doğrulama",
        ProductActivityType.PublishRequested => "Yayın Talebi",
        ProductActivityType.PublishSent => "Trendyol'a Gönderildi",
        ProductActivityType.BatchCompleted => "Batch Tamamlandı",
        ProductActivityType.BatchFailed => "Batch Başarısız",
        ProductActivityType.Approved => "Onaylandı",
        ProductActivityType.Rejected => "Reddedildi",
        ProductActivityType.Archived => "Arşivlendi",
        ProductActivityType.StockUpdated => "Stok Güncellendi",
        ProductActivityType.PriceUpdated => "Fiyat Güncellendi",
        ProductActivityType.ContentUpdated => "İçerik Güncellendi",
        ProductActivityType.ImageUpdated => "Görsel Güncellendi",
        ProductActivityType.Deleted => "Silindi",
        _ => type.ToString()
    };
}
