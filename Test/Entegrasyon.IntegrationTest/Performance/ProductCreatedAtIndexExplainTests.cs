using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Performance;

/// <summary>
/// PERF HIGH-2 (Task #7) — MainProducts.CreatedAt partial index.
///
/// DENETIM DUZELTMESI: Snapshot-only denetim "CreatedAt index'siz" demisti; ancak pg_indexes
/// ile dogrulandi ki raw-SQL partial index <c>IX_Products_CreatedAt_Desc_Active</c>
/// (CreatedAt DESC, WHERE NOT "IsDeleted" — AddDatabaseIndexes migration'i) ZATEN VAR ve
/// planlayici onu admin grid sort + export tarih filtresi icin KULLANIYOR. Yani Task #7
/// halihazirda karsilanmis durumda — yeni migration GEREKMIYOR.
///
/// Bu testler bir REGRESSION GUARD'dir: index ileride silinirse/bozulursa kirmizi yansin diye
/// EXPLAIN (50k satir) ile index'in iki kritik sorgu seklinde kullanildigini sabitler.
/// </summary>
[Trait("Category", "Integration")]
public class ProductCreatedAtIndexExplainTests : PerfIndexTestBase
{
    public ProductCreatedAtIndexExplainTests(PostgreSqlFixture pg) : base(pg) { }

    protected override string TableToAnalyze => "MainProducts";

    protected override async Task SeedAsync(IntegrationDbContext db)
    {
        var category = new Entegrasyon.Entity.Categories.Category
        {
            Name = "PerfTest Category",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var catId = category.Id;

        // SearchVector generated (tsvector) → insert'e dahil edilmez. 50k urun, CreatedAt zaman serisi.
        var sql = $"""
            INSERT INTO "MainProducts"
                ("Id","CreatedAt","UpdatedAt","DeletedAt","IsDeleted","Title","CategoryId","IsPreOrder","IsPublished")
            SELECT
                gen_random_uuid(),
                now() - (g || ' minutes')::interval,
                now(), now(), false,
                'Test Product ' || g,
                {catId},
                false, true
            FROM generate_series(0, 49999) AS g;
            """;
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    [Fact]
    public async Task Admin_Grid_DefaultSort_Uses_CreatedAt_Index()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            "SELECT \"Id\" FROM \"MainProducts\" WHERE NOT \"IsDeleted\" ORDER BY \"CreatedAt\" DESC LIMIT 20");

        plan.Should().Contain("IX_Products_CreatedAt_Desc_Active",
            $"Admin urun grid default sort (ProductManager:416/686/750) CreatedAt partial index'ini kullanmali. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"MainProducts\"");
    }

    [Fact]
    public async Task Export_DateRange_Uses_CreatedAt_Index()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            "SELECT \"Id\" FROM \"MainProducts\" WHERE NOT \"IsDeleted\" AND \"CreatedAt\" >= now() - interval '90 minutes' AND \"CreatedAt\" <= now()");

        plan.Should().Contain("IX_Products_CreatedAt_Desc_Active",
            $"Export tarih filtresi (BulkOperationManager:485/573/645) CreatedAt partial index'ini kullanmali. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"MainProducts\"");
    }
}
