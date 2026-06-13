using Entegrasyon.Business.Helpers;
using Entegrasyon.Entity.Categories;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Master katalog import "İçe Aktarma Başarısız: An error occurred while saving the entity changes"
/// regresyonu. CategoryAttributeValue unique filtered index (CategoryAttributeId, NormalizedName)
/// gereği import her değerin NormalizedName'ini ŞART set etmeli. Eski kod sadece Name set ediyordu →
/// NormalizedName="" → aynı attribute altındaki ikinci değer unique-violation ile SaveChanges'ı
/// patlatıyordu. Bu testler MasterCatalogImportService'in DB'ye yazma mekanizmasını birebir taklit eder.
///
/// NOT: Servisin kendisi ikinci bir DbContext (AdminPanelDbContext) gerektirir; o context test
/// harness'ında host edilmiyor. Bu yüzden test, servisin IntegrationDbContext'e yazdığı şekli
/// (CategoryAttribute + iki CategoryAttributeValue) doğrudan kurarak unique-index davranışını ve
/// fix'i (kanonik normalize) persist katmanında doğrular. Testcontainers/PostgreSQL gerektirir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MasterCatalogImportValueNormalizationTests : IntegrationTestBase
{
    public MasterCatalogImportValueNormalizationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    /// <summary>
    /// RED-first: import'un ESKİ davranışı — NormalizedName set edilmeden iki değer eklenince
    /// unique index (CategoryAttributeId, NormalizedName) çakışır ve SaveChanges DbUpdateException atar.
    /// Bu, kullanıcının gördüğü "saving the entity changes" hatasının kök sebebidir.
    /// </summary>
    [Fact]
    public async Task Two_values_with_empty_NormalizedName_violate_unique_index()
    {
        using var db = CreateDbContext();

        var attribute = new CategoryAttribute
        {
            CategoryAttributeKey = "color",
            CategoryAttributeHumanized = "Renk",
            ImportId = 1001,
            CategoryAttributeValues =
            {
                new CategoryAttributeValue { Name = "Kırmızı" }, // NormalizedName="" (eski hatalı kod)
                new CategoryAttributeValue { Name = "Mavi" }     // NormalizedName="" → çakışma
            }
        };
        db.CategoryAttributes.Add(attribute);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "boş NormalizedName'li iki değer (CategoryAttributeId, NormalizedName) unique index'ini ihlal eder");
    }

    /// <summary>
    /// GREEN: fix — her değerin NormalizedName'i kanonik normalize ile set edilirse import sorunsuz
    /// persist eder; farklı değerler farklı kanona düştüğü için unique index çakışmaz.
    /// </summary>
    [Fact]
    public async Task Values_with_canonical_NormalizedName_persist_successfully()
    {
        int attributeId;
        using (var db = CreateDbContext())
        {
            var attribute = new CategoryAttribute
            {
                CategoryAttributeKey = "color",
                CategoryAttributeHumanized = "Renk",
                ImportId = 2002,
                CategoryAttributeValues =
                {
                    new CategoryAttributeValue { Name = "Kırmızı", NormalizedName = AttributeValueNormalizer.Normalize("Kırmızı") },
                    new CategoryAttributeValue { Name = "Mavi", NormalizedName = AttributeValueNormalizer.Normalize("Mavi") }
                }
            };
            db.CategoryAttributes.Add(attribute);
            await db.SaveChangesAsync();
            attributeId = attribute.Id;
        }

        using var verify = CreateDbContext();
        var values = await verify.CategoryAttributeValues
            .AsNoTracking()
            .Where(v => v.CategoryAttributeId == attributeId)
            .ToListAsync();

        values.Should().HaveCount(2);
        values.Select(v => v.NormalizedName).Should().BeEquivalentTo(new[] { "KIRMIZI", "MAVİ" });
    }
}
