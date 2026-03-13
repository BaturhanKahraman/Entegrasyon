using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using static Entegrasyon.Blazor.Features.Products.ProductEdit;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditImagesTab
{
    [Parameter] public List<ImageEditState> Images { get; set; } = [];
    [Parameter] public List<IBrowserFile> NewImages { get; set; } = [];
    [Parameter] public EventCallback<List<IBrowserFile>> NewImagesChanged { get; set; }

    private void SetCover(ImageEditState img)
    {
        foreach (var i in Images) i.IsMain = false;
        img.IsMain = true;
    }

    private void MarkDeleted(ImageEditState img)
    {
        img.IsDeleted = true;
    }

    private async Task OnFilesChanged(IReadOnlyList<IBrowserFile> files)
    {
        var updated = new List<IBrowserFile>(NewImages);
        updated.AddRange(files);
        await NewImagesChanged.InvokeAsync(updated);
    }
}
