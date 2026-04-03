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

    public ImageManager(
        ILogger<ImageManager> logger,
        IImageProcessingService imageProcessing,
        IDbContextFactory<IntegrationDbContext> contextFactory)
    {
        _logger = logger;
        _imageProcessing = imageProcessing;
        _contextFactory = contextFactory;
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
}
