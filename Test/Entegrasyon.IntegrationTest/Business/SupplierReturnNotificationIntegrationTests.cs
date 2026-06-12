using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Tedarikçiye Yüksek İadeli Ürün Bildirimi (MUTASYON) — gerçek PostgreSQL.
///
/// Davranış:
///  - Brand.SupplierEmail tanımlı → e-posta yolu denenir (test ortamında SMTP yok → ErrorResult).
///  - Brand.SupplierEmail boş → in-app bildirime düşer, SuccessResult + Notification kaydı oluşur.
///  - İş kuralları: ürün yok / markası yok → ErrorResult.
/// </summary>
[Trait("Category", "Integration")]
public class SupplierReturnNotificationIntegrationTests : IntegrationTestBase
{
    public SupplierReturnNotificationIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private ISupplierReturnNotificationManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<ISupplierReturnNotificationManager>();
        scope = s;
        return svc;
    }

    private async Task<Guid> SeedVariantWithBrandAsync(string? supplierEmail, bool withBrand = true)
    {
        using var db = CreateDbContext();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();

        int? brandId = null;
        if (withBrand)
        {
            var brand = new Brand { Name = $"Marka-{Guid.NewGuid():N}"[..14], SupplierEmail = supplierEmail, CreatedAt = DateTimeOffset.UtcNow };
            db.Brands.Add(brand);
            await db.SaveChangesAsync();
            brandId = brand.Id;
        }

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = productId,
            Title = "Yüksek İadeli Ürün",
            StockCode = $"SC-{Guid.NewGuid():N}"[..10],
            BrandId = brandId,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.ProductVariants.Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = $"BC-{Guid.NewGuid():N}"[..10],
            ListPrice = 200,
            SalePrice = 180,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return variantId;
    }

    [Fact]
    public async Task NoSupplierEmail_FallsBackToInApp_AndSucceeds()
    {
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var variantId = await SeedVariantWithBrandAsync(supplierEmail: null);

        var sut = Sut(out var scope);
        using (scope)
        {
            var result = await sut.NotifyHighReturnAsync(
                new NotifySupplierHighReturnDto(variantId, 42.5), userId);

            Assert.True(result.Success);
            Assert.Contains("panele", result.Message);

            using var db = CreateDbContext();
            var notified = await db.Notifications.AnyAsync(n => n.Header!.Contains("Yüksek İade"));
            Assert.True(notified);
        }
    }

    [Fact]
    public async Task WithSupplierEmail_AttemptsEmail_NoSmtp_ReturnsError()
    {
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var variantId = await SeedVariantWithBrandAsync(supplierEmail: "tedarikci@example.com");

        var sut = Sut(out var scope);
        using (scope)
        {
            var result = await sut.NotifyHighReturnAsync(
                new NotifySupplierHighReturnDto(variantId, 30), userId);

            // SMTP yapılandırılmadığı için e-posta gönderilemez → e-posta yolunun seçildiğini kanıtlar.
            Assert.False(result.Success);
            Assert.Contains("gönderilemedi", result.Message);
        }
    }

    [Fact]
    public async Task ProductNotFound_ReturnsError()
    {
        await SeedBasicEntitiesAsync();
        var sut = Sut(out var scope);
        using (scope)
        {
            var result = await sut.NotifyHighReturnAsync(
                new NotifySupplierHighReturnDto(Guid.NewGuid(), 20), userId: null);

            Assert.False(result.Success);
            Assert.Contains("bulunamadı", result.Message);
        }
    }

    [Fact]
    public async Task BrandMissing_ReturnsError()
    {
        await SeedBasicEntitiesAsync();
        var variantId = await SeedVariantWithBrandAsync(supplierEmail: null, withBrand: false);

        var sut = Sut(out var scope);
        using (scope)
        {
            var result = await sut.NotifyHighReturnAsync(
                new NotifySupplierHighReturnDto(variantId, 20), userId: null);

            Assert.False(result.Success);
            Assert.Contains("marka", result.Message!.ToLowerInvariant());
        }
    }
}
