using Bunit.TestDoubles;
using Entegrasyon.Blazor.Features.Products;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.BunitTest.Features.Products;

public class AddProductTests : BunitBaseTest
{
    public AddProductTests()
    {
        // Varsayılan başarılı lookup mock'ları — her test ihtiyacına göre override edebilir
        MockBrandService
            .Setup(b => b.GetBrandListDetails())
            .ReturnsAsync(new SuccessDataResult<List<BrandListDetailDto>>([]));

        MockCategoryService
            .Setup(c => c.GetAllCategoriesWithoutAttributesAsync())
            .ReturnsAsync([]);
    }

    [Fact]
    public void Component_ShouldRender_WithoutErrors()
    {
        // Act & Assert — exception fırlatmamalı
        var act = () => RenderComponent<AddProduct>();
        act.Should().NotThrow();
    }

    [Fact]
    public void Component_ShouldInitiallyShowNextButton_AtStepZero()
    {
        // Act
        var cut = RenderComponent<AddProduct>();

        // Assert — "İleri" butonu görünür, "Kaydet ve Yayınla" görünmez
        cut.Markup.Should().Contain("İleri");
        cut.Markup.Should().NotContain("Kaydet ve Yayınla");
    }

    [Fact]
    public async Task Submit_WhenRequiredFieldsMissing_ShouldShowWarning()
    {
        // Arrange — step=3'e geç ama title/brandId/categoryId boş bırak
        var cut = RenderComponent<AddProduct>();
        await SetPrivateFieldAsync(cut, "stepIndex", 3);
        cut.Render(); // re-render ile "Kaydet ve Yayınla" butonunu göster

        // Act
        var submitButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Kaydet ve Yayınla"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        MockSnackbar.Verify(
            s => s.Add(It.IsAny<string>(), Severity.Warning, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
            Times.AtLeastOnce);
        MockProductService.Verify(p => p.AddProduct(It.IsAny<AddProductDto>()), Times.Never);
    }

    [Fact]
    public async Task Submit_WhenProductManagerReturnsError_ShouldShowError()
    {
        // Arrange
        MockProductService
            .Setup(p => p.AddProduct(It.IsAny<AddProductDto>()))
            .ReturnsAsync(new ErrorDataResult<Product>(null!, "Geçersiz varyant"));

        var cut = RenderComponent<AddProduct>();
        await SetPrivateFieldAsync(cut, "title", "Test Ürün");
        await SetPrivateFieldAsync(cut, "brandId", 1);
        await SetPrivateFieldAsync(cut, "categoryId", 1);
        await SetPrivateFieldAsync(cut, "stepIndex", 3);
        cut.Render();

        // Act
        var submitButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Kaydet ve Yayınla"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        MockSnackbar.Verify(
            s => s.Add(It.Is<string>(msg => msg.Contains("Geçersiz varyant")), Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_WhenSucceeds_ShouldNavigateToProductsPage()
    {
        // Arrange
        var savedProduct = new Product { Id = Guid.NewGuid(), ProductVariants = [] };
        MockProductService
            .Setup(p => p.AddProduct(It.IsAny<AddProductDto>()))
            .ReturnsAsync(new SuccessDataResult<Product>(savedProduct, "Ürün eklendi."));
        MockImageManager
            .Setup(i => i.AddProductImages(It.IsAny<Guid>(), It.IsAny<IEnumerable<VariantImageStream>>()))
            .ReturnsAsync(new SuccessResult());

        var cut = RenderComponent<AddProduct>();
        await SetPrivateFieldAsync(cut, "title", "Test Ürün");
        await SetPrivateFieldAsync(cut, "brandId", 1);
        await SetPrivateFieldAsync(cut, "categoryId", 1);
        await SetPrivateFieldAsync(cut, "stepIndex", 3);
        cut.Render();

        var navManager = Services.GetRequiredService<FakeNavigationManager>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Kaydet ve Yayınla"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        MockProductService.Verify(p => p.AddProduct(It.IsAny<AddProductDto>()), Times.Once);
        navManager.Uri.Should().EndWith("/products");
    }

    [Fact]
    public async Task Submit_WhenSucceeds_ShouldCallProductManager_WithCorrectTitle()
    {
        // Arrange
        var savedProduct = new Product { Id = Guid.NewGuid(), ProductVariants = [] };
        AddProductDto? capturedDto = null;
        MockProductService
            .Setup(p => p.AddProduct(It.IsAny<AddProductDto>()))
            .Callback<AddProductDto>(dto => capturedDto = dto)
            .ReturnsAsync(new SuccessDataResult<Product>(savedProduct, "Ürün eklendi."));
        MockImageManager
            .Setup(i => i.AddProductImages(It.IsAny<Guid>(), It.IsAny<IEnumerable<VariantImageStream>>()))
            .ReturnsAsync(new SuccessResult());

        var cut = RenderComponent<AddProduct>();
        await SetPrivateFieldAsync(cut, "title", "Spesifik Başlık");
        await SetPrivateFieldAsync(cut, "brandId", 5);
        await SetPrivateFieldAsync(cut, "categoryId", 3);
        await SetPrivateFieldAsync(cut, "stepIndex", 3);
        cut.Render();

        // Act
        var submitButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Kaydet ve Yayınla"));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Title.Should().Be("Spesifik Başlık");
        capturedDto.BrandId.Should().Be(5);
        capturedDto.CategoryId.Should().Be(3);
    }

    [Fact]
    public async Task GenerateBarcodeButton_ShouldCallBarcodeService()
    {
        // Arrange
        MockBarcodeService
            .Setup(b => b.GenerateAsync())
            .ReturnsAsync("9780000000001");

        var cut = RenderComponent<AddProduct>();

        // Variants listesine doğrudan eleman ekle
        await cut.InvokeAsync(() =>
        {
            var variantsField = typeof(AddProduct).GetField("variants", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var variantsList = (List<AddProductVariantDto>)variantsField.GetValue(cut.Instance)!;
            variantsList.Add(new AddProductVariantDto { Barcode = string.Empty, BranchOfficeStocks = [] });
        });

        // Step 2'ye geç (Varyant Detayları)
        await SetPrivateFieldAsync(cut, "stepIndex", 2);
        cut.Render();

        // Act — "Barkod Oluştur" adornment butonunu tıkla
        var barcodeAdornment = cut.FindAll("button[aria-label='Barkod Oluştur']");
        barcodeAdornment.Should().NotBeEmpty("Barkod üret butonu render edilmeli");
        await cut.InvokeAsync(() => barcodeAdornment.First().Click());

        // Assert
        MockBarcodeService.Verify(b => b.GenerateAsync(), Times.Once);
    }
}
