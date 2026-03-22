using Entegrasyon.Entity.Labels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Settings.LabelDesigner;

public partial class LabelElementProperties : ComponentBase
{
    [Parameter] public LabelElement? Element { get; set; }
    [Parameter] public EventCallback<LabelElement> OnChanged { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }

    private async Task UpdateProperty(Action<LabelElement> update)
    {
        if (Element is null) return;
        update(Element);
        await OnChanged.InvokeAsync(Element);
    }

    private static string GetElementTypeLabel(string type) => type switch
    {
        "Title" => "Başlık",
        "VariantInfo" => "Varyant Bilgisi",
        "Barcode" => "Barkod",
        "SalePrice" => "Satış Fiyatı",
        "ListPrice" => "Liste Fiyatı",
        "CustomText" => "Özel Yazı",
        "Line" => "Çizgi",
        _ => type
    };
}
