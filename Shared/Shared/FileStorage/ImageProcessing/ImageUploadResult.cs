namespace Shared.FileStorage.ImageProcessing;

public record ImageUploadResult(
    string BaseKey,          // "{productId}/{variantId}/{guid}" — stored in Image.StorageKey
    string OriginalUrl,
    string MediumUrl,
    string ThumbUrl,
    int Width,
    int Height,
    long FileSizeBytes,
    string ContentType = "image/webp");
