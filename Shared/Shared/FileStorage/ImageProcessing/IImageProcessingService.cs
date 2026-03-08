namespace Shared.FileStorage.ImageProcessing;

public interface IImageProcessingService
{
    /// <summary>
    /// Resizes the image to three variants (original/medium/thumb),
    /// converts all to WebP and uploads them to MinIO.
    /// </summary>
    Task<ImageUploadResult> ProcessAndUploadAsync(
        Stream imageStream,
        string productId,
        string variantId);
}
