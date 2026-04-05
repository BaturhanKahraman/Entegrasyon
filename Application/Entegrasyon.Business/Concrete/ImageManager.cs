using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class ImageManager : IImageManager
{
    private readonly ILogger<ImageManager> _logger;
    private readonly IImageProcessingService _imageProcessing;
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly IMinioFileStorage _minio;

    public ImageManager(
        ILogger<ImageManager> logger,
        IImageProcessingService imageProcessing,
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IMinioFileStorage minio)
    {
        _logger = logger;
        _imageProcessing = imageProcessing;
        _contextFactory = contextFactory;
        _minio = minio;
    }

    public async Task<IResult> AddProductImages(Guid productId, IEnumerable<VariantImageStream> images)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var entities = new List<Image>();
        var displayOrderByVariant = new Dictionary<Guid, int>();

        foreach (var img in images)
        {
            try
            {
                var result = await _imageProcessing.ProcessAndUploadAsync(
                    img.ImageStream,
                    productId.ToString(),
                    img.VariantId.ToString());

                if (!displayOrderByVariant.TryGetValue(img.VariantId, out int order))
                    order = 0;

                entities.Add(new Image
                {
                    ProductVariantId = img.VariantId,
                    StorageKey = result.BaseKey,
                    FileStorageType = FileStorageType.Minio,
                    OriginalWidth = result.Width,
                    OriginalHeight = result.Height,
                    FileSizeBytes = result.FileSizeBytes,
                    ContentType = result.ContentType,
                    DisplayOrder = order,
                    IsMain = img.IsMain,
                    AlternativeText = img.FileName,
                    ThumbnailGenerated = true,
                    MediumGenerated = true,
                    Src = result.OriginalUrl
                });

                displayOrderByVariant[img.VariantId] = order + 1;

                _logger.LogInformation("Image uploaded: {BaseKey}, {W}x{H}, {Bytes} bytes",
                    result.BaseKey, result.Width, result.Height, result.FileSizeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image: {FileName}", img.FileName);
            }
        }

        if (entities.Count > 0)
        {
            await dbContext.Images.AddRangeAsync(entities);
            await dbContext.SaveChangesAsync();
        }

        return new SuccessResult($"{entities.Count} görsel yüklendi.");
    }

    public async Task<IResult> SoftDeleteVariantImages(Guid variantId)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var images = await dbContext.Images
            .AsTracking()
            .Where(i => i.ProductVariantId == variantId && !i.IsDeleted)
            .ToListAsync();

        if (images.Count == 0)
            return new SuccessResult("Silinecek görsel bulunamadı.");

        foreach (var image in images)
        {
            image.IsDeleted = true;
            image.DeletedAt = DateTimeOffset.UtcNow;

            // StorageKey başka bir aktif Image tarafından kullanılıyorsa MinIO'dan silme
            if (!string.IsNullOrEmpty(image.StorageKey))
            {
                var isShared = await dbContext.Images
                    .AnyAsync(i => i.StorageKey == image.StorageKey
                                && i.Id != image.Id
                                && !i.IsDeleted);

                if (!isShared)
                {
                    try
                    {
                        await _minio.DeleteAsync($"{image.StorageKey}_original.webp");
                        await _minio.DeleteAsync($"{image.StorageKey}_medium.webp");
                        await _minio.DeleteAsync($"{image.StorageKey}_thumb.webp");
                        _logger.LogInformation("MinIO images deleted for key: {StorageKey}", image.StorageKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete MinIO images for key: {StorageKey}", image.StorageKey);
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult($"{images.Count} görsel silindi.");
    }

    public async Task<IResult> CloneImagesToVariant(Guid targetVariantId, List<int> sourceImageIds)
    {
        if (sourceImageIds.Count == 0)
            return new SuccessResult("Kopyalanacak görsel seçilmedi.");

        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var sourceImages = await dbContext.Images
            .AsNoTracking()
            .Where(i => sourceImageIds.Contains(i.Id) && !i.IsDeleted)
            .ToListAsync();

        var clones = sourceImages.Select((src, idx) => new Image
        {
            ProductVariantId = targetVariantId,
            StorageKey = src.StorageKey,
            FileStorageType = src.FileStorageType,
            OriginalWidth = src.OriginalWidth,
            OriginalHeight = src.OriginalHeight,
            FileSizeBytes = src.FileSizeBytes,
            ContentType = src.ContentType,
            DisplayOrder = idx,
            IsMain = false,
            AlternativeText = src.AlternativeText,
            ThumbnailGenerated = src.ThumbnailGenerated,
            MediumGenerated = src.MediumGenerated,
            Src = src.Src
        }).ToList();

        await dbContext.Images.AddRangeAsync(clones);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Cloned {Count} images to variant {VariantId}", clones.Count, targetVariantId);
        return new SuccessResult($"{clones.Count} görsel kopyalandı.");
    }
}
