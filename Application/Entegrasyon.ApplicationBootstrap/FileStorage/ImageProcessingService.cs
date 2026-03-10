using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

using Entegrasyon.Business.FileStorage;

namespace Entegrasyon.ApplicationBootstrap.FileStorage;

public class ImageProcessingService : IImageProcessingService
{
    private const int MaxOriginalDimension = 2000;
    private const int MediumMaxDimension = 400;
    private const int ThumbMaxDimension = 150;
    private const int WebPQuality = 82;

    private readonly IMinioFileStorage _minio;

    public ImageProcessingService(IMinioFileStorage minio)
    {
        _minio = minio;
    }

    public async Task<ImageUploadResult> ProcessAndUploadAsync(
        Stream imageStream,
        string productId,
        string variantId)
    {
        using var image = await Image.LoadAsync(imageStream);

        int originalWidth = image.Width;
        int originalHeight = image.Height;

        var guid = Guid.NewGuid().ToString("N");
        string baseKey = $"{productId}/{variantId}/{guid}";

        var encoder = new WebpEncoder { Quality = WebPQuality };

        // Upload all 3 variants concurrently
        var originalTask = UploadVariantAsync(image, baseKey, "original", MaxOriginalDimension, encoder);
        var mediumTask   = UploadVariantAsync(image, baseKey, "medium",   MediumMaxDimension,  encoder);
        var thumbTask    = UploadVariantAsync(image, baseKey, "thumb",    ThumbMaxDimension,   encoder);

        var (originalUrl, originalBytes) = await originalTask;
        var (mediumUrl, _)               = await mediumTask;
        var (thumbUrl, _)                = await thumbTask;

        return new ImageUploadResult(
            BaseKey: baseKey,
            OriginalUrl: originalUrl,
            MediumUrl: mediumUrl,
            ThumbUrl: thumbUrl,
            Width: originalWidth,
            Height: originalHeight,
            FileSizeBytes: originalBytes);
    }

    private async Task<(string Url, long Bytes)> UploadVariantAsync(
        Image source,
        string baseKey,
        string suffix,
        int maxDimension,
        WebpEncoder encoder)
    {
        using var clone = source.Clone(ctx =>
        {
            if (ctx.GetCurrentSize().Width > maxDimension || ctx.GetCurrentSize().Height > maxDimension)
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxDimension, maxDimension),
                    Mode = ResizeMode.Max
                });
        });

        using var ms = new MemoryStream();
        await clone.SaveAsync(ms, encoder);
        ms.Seek(0, SeekOrigin.Begin);

        string objectName = $"{baseKey}_{suffix}.webp";
        string url = await _minio.UploadAsync(ms, objectName, "image/webp");
        return (url, ms.Length);
    }
}
