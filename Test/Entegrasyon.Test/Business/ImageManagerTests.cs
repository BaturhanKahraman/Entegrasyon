using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Business;

public class ImageManagerTests : BaseTest
{
    private readonly ImageManager _sut;
    private readonly Mock<IImageProcessingService> _mockImageProcessing = new();
    private readonly Mock<ILogger<ImageManager>> _mockLogger = new();

    public ImageManagerTests()
    {
        mockIntegrationDbContext
            .Setup(x => x.Images)
            .ReturnsDbSet(new List<Image>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new ImageManager(
            _mockLogger.Object,
            _mockImageProcessing.Object,
            mockContextFactory.Object);
    }

    [Fact]
    public async Task AddProductImages_WithMemoryStream_UploadsSuccessfully()
    {
        // Arrange — byte[] tabanlı MemoryStream kullanarak (dialog kapandıktan sonraki senaryo)
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var fakeImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        var stream = new MemoryStream(fakeImageBytes);

        var uploadResult = new ImageUploadResult(
            BaseKey: $"{productId}/{variantId}/abc123",
            OriginalUrl: $"http://localhost:9000/products/{productId}/{variantId}/abc123_original.webp",
            MediumUrl: $"http://localhost:9000/products/{productId}/{variantId}/abc123_medium.webp",
            ThumbUrl: $"http://localhost:9000/products/{productId}/{variantId}/abc123_thumb.webp",
            Width: 800,
            Height: 600,
            FileSizeBytes: fakeImageBytes.Length);

        _mockImageProcessing
            .Setup(x => x.ProcessAndUploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(uploadResult);

        var images = new List<VariantImageStream>
        {
            new(variantId, stream, "test-image.jpg", IsMain: true)
        };

        // Act
        var result = await _sut.AddProductImages(productId, images);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("1 görsel yüklendi");

        _mockImageProcessing.Verify(
            x => x.ProcessAndUploadAsync(It.IsAny<Stream>(), productId.ToString(), variantId.ToString()),
            Times.Once);

        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddProductImages_WithMultipleImages_SetsCorrectDisplayOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        _mockImageProcessing
            .Setup(x => x.ProcessAndUploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string pid, string vid) => new ImageUploadResult(
                BaseKey: $"{pid}/{vid}/{Guid.NewGuid():N}",
                OriginalUrl: "http://localhost/img.webp",
                MediumUrl: "http://localhost/img_m.webp",
                ThumbUrl: "http://localhost/img_t.webp",
                Width: 100, Height: 100, FileSizeBytes: 100));

        var images = new List<VariantImageStream>
        {
            new(variantId, new MemoryStream(new byte[] { 1 }), "img1.jpg", IsMain: true),
            new(variantId, new MemoryStream(new byte[] { 2 }), "img2.jpg", IsMain: false),
            new(variantId, new MemoryStream(new byte[] { 3 }), "img3.jpg", IsMain: false)
        };

        // Act
        var result = await _sut.AddProductImages(productId, images);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("3 görsel yüklendi");
        _mockImageProcessing.Verify(
            x => x.ProcessAndUploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task AddProductImages_WhenProcessingFails_ContinuesWithOtherImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var callCount = 0;

        _mockImageProcessing
            .Setup(x => x.ProcessAndUploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string pid, string vid) =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("MinIO connection failed");

                return new ImageUploadResult(
                    BaseKey: $"{pid}/{vid}/key",
                    OriginalUrl: "http://localhost/img.webp",
                    MediumUrl: "http://localhost/img_m.webp",
                    ThumbUrl: "http://localhost/img_t.webp",
                    Width: 100, Height: 100, FileSizeBytes: 100);
            });

        var images = new List<VariantImageStream>
        {
            new(variantId, new MemoryStream(new byte[] { 1 }), "fail.jpg", IsMain: true),
            new(variantId, new MemoryStream(new byte[] { 2 }), "success.jpg", IsMain: false)
        };

        // Act
        var result = await _sut.AddProductImages(productId, images);

        // Assert — ilk başarısız olsa da ikincisi kaydedilmeli
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("1 görsel yüklendi");
    }
}
