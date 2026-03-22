using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using static Entegrasyon.Blazor.Features.Products.ProductEdit;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditImagesTab
{
    [Parameter] public List<ImageEditState> Images { get; set; } = [];
    [Parameter] public List<BufferedImage> NewImages { get; set; } = [];
    [Parameter] public EventCallback<List<BufferedImage>> NewImagesChanged { get; set; }

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
        var updated = new List<BufferedImage>(NewImages);
        foreach (var file in files)
        {
            if (file.Size > 10_485_760) continue; // skip > 10MB
            using var ms = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 10_485_760).CopyToAsync(ms);
            updated.Add(new BufferedImage
            {
                FileData = ms.ToArray(),
                FileName = file.Name
            });
        }
        await NewImagesChanged.InvokeAsync(updated);
    }
}
