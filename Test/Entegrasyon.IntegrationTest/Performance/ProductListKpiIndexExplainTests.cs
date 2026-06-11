using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Performance;

/// <summary>
/// Ürünler liste header KPI sorgusu (ProductManager.GetProductListKpiAsync) için EXPLAIN
/// regresyon-guard'ı.
///
/// KPI tek-sorgu aggregate'i MainProducts → ProductVariants (ProductId join) → BranchOfficeStocks
/// üzerinden çalışır. Kullanıcının açık kaygısı: "veritabanını kilitlememesi, canlıda başımız
/// ağrımasın". Bu test, hot tablo ProductVariants'taki ProductId join'inin yeni partial index
/// <c>IX_ProductVariants_ProductId_Active</c>'i kullandığını ve büyük veride seq-scan'e düşmediğini
/// sabitler. Index silinirse/bozulursa kırmızı yansın.
/// </summary>
[Trait("Category", "Integration")]
public class ProductListKpiIndexExplainTests : PerfIndexTestBase
{
    public ProductListKpiIndexExplainTests(PostgreSqlFixture pg) : base(pg) { }

    protected override string TableToAnalyze => "ProductVariants";

    protected override async Task SeedAsync(IntegrationDbContext db)
    {
        var category = new Entegrasyon.Entity.Categories.Category
        {
            Name = "KPI Perf Category",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var catId = category.Id;

        // BranchOfficeStocks FK → BranchOffice; varyant/stok insert'inden ÖNCE seed et.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "BranchOffices" ("Id","Name","IsDefaultMarketPlaceStock","CreatedAt","UpdatedAt","DeletedAt","IsDeleted")
            VALUES (1,'Ana Depo',true,now(),now(),now(),false)
            ON CONFLICT ("Id") DO NOTHING;
            """);

        // 20k ürün + her birine 1 varyant + 1 branch stock. ProductId join'i için yeterli kardinalite.
        // catId kod-içi int (kullanıcı girdisi değil); değişkene alıp ExecuteSqlRaw'a vererek
        // (mevcut PerfIndex test deseni) EF1002/1003 analyzer'ı tetiklenmez.
        var seedSql = $"""
            WITH new_products AS (
                INSERT INTO "MainProducts"
                    ("Id","CreatedAt","UpdatedAt","DeletedAt","IsDeleted","Title","CategoryId","IsPreOrder","IsPublished")
                SELECT gen_random_uuid(), now(), now(), now(), false,
                    'KPI Perf ' || g, {catId}, false, true
                FROM generate_series(0, 19999) AS g
                RETURNING "Id"
            ),
            new_variants AS (
                INSERT INTO "ProductVariants"
                    ("Id","ProductId","CreatedAt","UpdatedAt","DeletedAt","IsDeleted","CurrencyType")
                SELECT gen_random_uuid(), "Id", now(), now(), now(), false, 'TRY'
                FROM new_products
                RETURNING "Id"
            )
            INSERT INTO "BranchOfficeStocks"
                ("BranchOfficeId","ProductVariantId","FirstTotalStock","SoldQuantity")
            SELECT 1, "Id", (random()*10)::int, 0
            FROM new_variants;
            """;
        await db.Database.ExecuteSqlRawAsync(seedSql);

        // BranchOfficeStocks FK → BranchOffice; seed et.
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "BranchOffices" ("Id","Name","IsDefaultMarketPlaceStock","CreatedAt","UpdatedAt","DeletedAt","IsDeleted")
            VALUES (1,'Ana Depo',true,now(),now(),now(),false)
            ON CONFLICT ("Id") DO NOTHING;
            """);

        await AnalyzeExtraAsync(db);
    }

    private static async Task AnalyzeExtraAsync(IntegrationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("ANALYZE \"MainProducts\";");
        await db.Database.ExecuteSqlRawAsync("ANALYZE \"BranchOfficeStocks\";");
    }

    [Fact]
    public async Task Kpi_VariantJoin_Uses_ProductId_Partial_Index()
    {
        await using var db = CreateDbContext();

        // GetProductListKpiAsync'in çekirdek CTE'si (per-product stok aggregate).
        var plan = await ExplainHelper.ExplainAsync(db, """
            SELECT COALESCE(SUM(current_stock),0)::int
            FROM (
                SELECT p."Id" AS pid, COALESCE(SUM(bos."CurrentStock"),0)::int AS current_stock
                FROM "MainProducts" p
                LEFT JOIN "ProductVariants" pv ON pv."ProductId" = p."Id" AND NOT pv."IsDeleted"
                LEFT JOIN "BranchOfficeStocks" bos ON bos."ProductVariantId" = pv."Id"
                WHERE NOT p."IsDeleted"
                GROUP BY p."Id"
            ) s
            """);

        plan.Should().Contain("IX_ProductVariants_ProductId_Active",
            $"KPI aggregate ProductVariants.ProductId join'inde yeni partial index'i kullanmalı. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"ProductVariants\"",
            $"Hot tablo ProductVariants'ta seq-scan olmamalı. Plan:\n{plan}");
    }
}
