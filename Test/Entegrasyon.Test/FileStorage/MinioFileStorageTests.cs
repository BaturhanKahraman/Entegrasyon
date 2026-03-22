using Entegrasyon.ApplicationBootstrap.FileStorage;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Entegrasyon.UnitTest.FileStorage;

public class MinioFileStorageTests
{
    private readonly MinioFileStorage _sut;
    private const string PublicBaseUrl = "http://192.168.1.78:9000";
    private const string BucketName = "products";

    public MinioFileStorageTests()
    {
        var options = Options.Create(new MinioOptions
        {
            Endpoint = "192.168.1.78:9000",
            AccessKey = "test",
            SecretKey = "test",
            UseSSL = false,
            BucketName = BucketName,
            PublicBaseUrl = PublicBaseUrl
        });

        _sut = new MinioFileStorage(options);
    }

    [Fact]
    public void GetPublicUrl_WithStorageKey_ReturnsFullUrl()
    {
        // Arrange
        var storageKey = "product-id/variant-id/image_original.webp";

        // Act
        var result = _sut.GetPublicUrl(storageKey);

        // Assert
        result.Should().Be($"{PublicBaseUrl}/{BucketName}/{storageKey}");
    }

    [Fact]
    public void GetPublicUrl_WithAlreadyFullUrl_DoesNotDoublePrefixUrl()
    {
        // Arrange — Src alanı zaten tam URL içeriyor
        var alreadyFullUrl = $"{PublicBaseUrl}/{BucketName}/product-id/variant-id/image_original.webp";

        // Act
        var result = _sut.GetPublicUrl(alreadyFullUrl);

        // Assert — çift sarmalama olmamalı
        result.Should().Be(alreadyFullUrl);
        result.Should().NotContain($"{PublicBaseUrl}/{BucketName}/{PublicBaseUrl}");
    }

    [Fact]
    public void GetPublicUrl_WithHttpsUrl_DoesNotDoublePrefixUrl()
    {
        // Arrange — farklı bir domain'den gelen URL
        var externalUrl = "https://cdn.example.com/images/product.webp";

        // Act
        var result = _sut.GetPublicUrl(externalUrl);

        // Assert
        result.Should().Be(externalUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetPublicUrl_WithNullOrEmpty_ReturnsEmptyString(string? objectName)
    {
        // Act
        var result = _sut.GetPublicUrl(objectName!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPublicUrl_WithBaseKeyPlusSuffix_ReturnsCorrectUrl()
    {
        // Arrange — StorageKey (baseKey) + _original.webp suffix ile doğru URL üretilmeli
        var baseKey = "f29b1628-7ab0-4828-be84-767edbb15950/802abcec-ab19-4863-b52c-9af8cd76fdf9/1f18ae61eb194629a59d802406b55e49";
        var objectName = $"{baseKey}_original.webp";

        // Act
        var result = _sut.GetPublicUrl(objectName);

        // Assert — tam MinIO URL'i
        result.Should().Be($"{PublicBaseUrl}/{BucketName}/{objectName}");
        result.Should().EndWith("_original.webp");
    }

    [Fact]
    public void GetPublicUrl_WithBaseKeyWithoutSuffix_ProducesNonExistentUrl()
    {
        // Arrange — suffix olmadan StorageKey kullanılırsa MinIO'da dosya bulunamaz
        var baseKeyOnly = "f29b1628-7ab0-4828-be84-767edbb15950/802abcec-ab19-4863-b52c-9af8cd76fdf9/1f18ae61eb194629a59d802406b55e49";

        // Act
        var result = _sut.GetPublicUrl(baseKeyOnly);

        // Assert — URL üretilir ama .webp ile bitmez (MinIO'da NoSuchKey verecek)
        result.Should().NotEndWith(".webp");
    }
}
