using Entegrasyon.Business.Abstract;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// ProductManager SEO slug normalizasyonu — Türkçe karakterli slug kaydedilirken
/// SlugHelper.GenerateSlug ile ASCII'ye indirgenmeli (ş→s, ı→i, ğ→g, lowercase, tire).
/// UpdateStoreSettings + PublishProduct yolları DB'ye persist ettiği için integration seviyesinde doğrulanır.
/// </summary>
[Trait("Category", "Integration")]
public class ProductSlugNormalizationIntegrationTests : IntegrationTestBase
{
    public ProductSlugNormalizationIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task<Guid> SeedProductAsync()
    {
        await SeedBasicEntitiesAsync();
        var (productId, _) = await SeedProductWithStockAsync("SLUG-BC-1", stock: 5);
        return productId;
    }

    private async Task<string?> ReadSlugAsync(Guid productId)
    {
        using var db = CreateDbContext();
        return await db.MainProducts.Where(p => p.Id == productId)
            .Select(p => p.SeoSlug).FirstAsync();
    }

    [Fact]
    public async Task UpdateStoreSettings_TurkceSlug_AsciiyeNormalizeEder()
    {
        var productId = await SeedProductAsync();
        var sut = GetService<IProductService>();

        var result = await sut.UpdateStoreSettings(
            productId, "Başlık", "Açıklama", "Kırmızı Şömine", "anahtar", new Dictionary<Guid, decimal>());

        Assert.True(result.Success);
        Assert.Equal("kirmizi-somine", await ReadSlugAsync(productId));
    }

    [Fact]
    public async Task PublishProduct_TurkceSlug_AsciiyeNormalizeEder()
    {
        var productId = await SeedProductAsync();
        var sut = GetService<IProductService>();

        var result = await sut.PublishProduct(
            productId, "Başlık", "Açıklama", "Çocuk Güneş Gözlüğü", "anahtar", new Dictionary<Guid, decimal>());

        Assert.True(result.Success);
        Assert.Equal("cocuk-gunes-gozlugu", await ReadSlugAsync(productId));
    }
}
