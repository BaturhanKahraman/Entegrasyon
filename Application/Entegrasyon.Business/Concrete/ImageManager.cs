using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Shared.FileStorage;
using Shared.FileStorage.ImageProcessing;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class ImageManager : IImageManager
{
    private readonly ILogger<ImageManager> _logger;
    private readonly IImageProcessingService _imageProcessing;
    private readonly IntegrationDbContext _dbContext;

    public ImageManager(
        ILogger<ImageManager> logger,
        IImageProcessingService imageProcessing,
        IntegrationDbContext dbContext)
    {
        _logger = logger;
        _imageProcessing = imageProcessing;
        _dbContext = dbContext;
    }

    public async Task<IResult> AddProductImages(AddProductDto dto, Product addedProduct)
    {
        var images = new List<Image>();

        foreach (var productVariant in addedProduct.ProductVariants)
        {
            var variantDto = dto.ProductVariants
                .FirstOrDefault(x => x.Barcode == productVariant.Barcode);

            if (variantDto?.UploadedImages == null || !variantDto.UploadedImages.Any())
                continue;

            var displayOrder = 0;
            foreach (var uploadedImage in variantDto.UploadedImages)
            {
                try
                {
                    await using var stream = uploadedImage.UploadedImageFile.OpenReadStream();
                    var result = await _imageProcessing.ProcessAndUploadAsync(
                        stream,
                        addedProduct.Id.ToString(),
                        productVariant.Id.ToString());

                    images.Add(new Image
                    {
                        ProductVariantId = productVariant.Id,
                        StorageKey = result.BaseKey,
                        FileStorageType = FileStorageType.Minio,
                        OriginalWidth = result.Width,
                        OriginalHeight = result.Height,
                        FileSizeBytes = result.FileSizeBytes,
                        ContentType = result.ContentType,
                        DisplayOrder = displayOrder,
                        IsMain = displayOrder == 0,
                        AlternativeText = uploadedImage.UploadedImageFile.FileName,
                        ThumbnailGenerated = true,
                        MediumGenerated = true,
                        Src = result.OriginalUrl,
                        IsCoverImage = displayOrder == 0
                    });

                    displayOrder++;
                    _logger.LogInformation("Image uploaded: {BaseKey}, Size: {W}x{H}, {Bytes} bytes",
                        result.BaseKey, result.Width, result.Height, result.FileSizeBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload image: {FileName}",
                        uploadedImage.UploadedImageFile.FileName);
                }
            }
        }

        if (images.Count > 0)
        {
            await _dbContext.Images.AddRangeAsync(images);
            await _dbContext.SaveChangesAsync();
        }

        return new SuccessResult($"{images.Count} images uploaded successfully.");
    }
}
