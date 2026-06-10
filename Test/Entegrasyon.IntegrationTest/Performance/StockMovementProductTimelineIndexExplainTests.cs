using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Performance;

/// <summary>
/// PERF (ürün-360 stok timeline) — StockMovements ProductVariantId + CreatedAt composite index.
///
/// Denetim bulgusu (DB Master review, ProductActivityPageManager.GetStockMovementsAsync):
/// Sorgu şekli "WHERE NOT IsDeleted AND ProductVariantId IN (ürünün variant'ları)
/// ORDER BY CreatedAt DESC LIMIT 100". Mevcut (ProductVariantId, BranchOfficeId) composite
/// filtreyi karşılıyor (seq scan YOK) ama CreatedAt sırasını veremiyor → planlayıcı ürünün
/// TÜM stok hareketlerini çekip SORT ediyor. StockMovements append-only audit, sınırsız büyür
/// → yüksek-hareketli üründe sort maliyeti zamanla artar.
///
/// Yeni partial index (ProductVariantId, CreatedAt DESC) WHERE NOT IsDeleted filter+sort'u tek
/// index'le karşılar → SORT düğümü kalkar, LIMIT 100 index range scan ile erken durur.
/// Index eklenmeden ÖNCE RED (Sort node), sonra GREEN (index scan, Sort yok).
///
/// FK izolasyonu: StockMovements.ProductVariantId/BranchOfficeId gerçek FK (Restrict). Bu test
/// SADECE StockMovements-taraf erişim planını (index scan + sort eliminasyonu) ölçer; join'i
/// değil. Seed, session_replication_role=replica ile FK trigger'larını bypass edip sentetik
/// ProductVariantId GUID'leriyle 50k satır yazar — planlayıcı için tablo şekli gerçekçidir.
/// </summary>
[Trait("Category", "Integration")]
public class StockMovementProductTimelineIndexExplainTests : PerfIndexTestBase
{
    // Hedef ürünün variant'ları (round-robin bucket 0/1/2'ye denk gelen sabit GUID'ler).
    private const string VariantA = "00000000-0000-0000-0000-000000000000";
    private const string VariantB = "00000000-0000-0000-0000-000000000001";
    private const string VariantC = "00000000-0000-0000-0000-000000000002";

    public StockMovementProductTimelineIndexExplainTests(PostgreSqlFixture pg) : base(pg) { }

    protected override string TableToAnalyze => "StockMovements";

    protected override async Task SeedAsync(IntegrationDbContext db)
    {
        // Gerçekçi ölçek + dağılım: 300k stok hareketi (büyük append-only audit tablosu).
        //  - Hedef variant'lar A/B/C: g%50==0 satırları g%3 ile A/B/C'ye gider → her biri ~2000
        //    hareket, TÜM zaman aralığına serpiştirilmiş. Selectivity tek variant ≈ %0.67,
        //    üç variant ≈ %2 (büyük tabloda bir ürün KÜÇÜK pay — gerçek dünya).
        //  - Geri kalan ~294k satır: 3000 farklı noise variant'a yayılır (~98 hareket/variant).
        // Bu rejimde:
        //   * CreatedAt-geriye-tarama: hedefin payı küçük → 100 satır için tablonun büyük kısmını
        //     tarar (pahalı).
        //   * Mevcut (ProductVariantId, BranchOfficeId)+Sort: hedefin ~2000 satırını çekip sort
        //     eder (pahalı).
        //   * Composite (ProductVariantId, CreatedAt DESC): hedefe doğrudan seek + sıra hazır,
        //     LIMIT 100 erken durur → planlayıcı bunu seçer.
        // Identity "Id" otomatik; FK trigger'ları seed boyunca devre dışı (sadece index planı ölçülür).
        const string sql = """
            SET session_replication_role = replica;

            INSERT INTO "StockMovements"
                ("CreatedAt","UpdatedAt","DeletedAt","IsDeleted",
                 "BranchOfficeId","ProductVariantId","Type","Quantity","StockBefore","StockAfter")
            SELECT
                now() - (g || ' seconds')::interval, now(), now(), false,
                1,
                CASE
                    WHEN g % 50 = 0
                        THEN ('00000000-0000-0000-0000-' || lpad((g % 3)::text, 12, '0'))::uuid
                    ELSE ('00000000-0000-0000-0000-' || lpad((100 + (g % 3000))::text, 12, '0'))::uuid
                END,
                0, 1, 0, 1
            FROM generate_series(0, 299999) AS g;

            SET session_replication_role = origin;
            """;
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Tek variant timeline (deterministik): index CreatedAt sırasını sağladığından SORT düğümü olmamalı.
    /// </summary>
    [Fact]
    public async Task SingleVariant_Timeline_UsesIndex_NoSort()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            $"SELECT \"Id\" FROM \"StockMovements\" WHERE NOT \"IsDeleted\" " +
            $"AND \"ProductVariantId\" = '{VariantA}'::uuid ORDER BY \"CreatedAt\" DESC LIMIT 100");

        plan.Should().Contain("IX_StockMovements_ProductVariantId_CreatedAt",
            $"Ürün-360 stok timeline (ProductActivityPageManager.GetStockMovementsAsync) filter+sort index'i kullanmalı. Plan:\n{plan}");
        plan.Should().NotContain("Sort",
            $"Composite (ProductVariantId, CreatedAt DESC) sırayı sağladığından ayrı Sort düğümü olmamalı. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"StockMovements\"");
    }

    /// <summary>
    /// Gerçek sorgu şekli: ürünün birden çok variant'ı (ANY). Bu durumda sorgunun INDEX-tabanlı
    /// kaldığını (büyük tabloda Seq Scan YOK) doğrular.
    ///
    /// DÜRÜSTLÜK NOTU (EXPLAIN ile ölçüldü): Çok-variant ANY için planlayıcının seçimi MALİYET-bağımlı.
    /// Test DB'sinde StockMovements.CreatedAt üzerinde btree (IX_StockMovements_CreatedAt) mevcut
    /// (config kaynaklı; PostgresOptimizations'ın BRIN'e çevirme adımı fresh-migrate'te etkisiz —
    /// bilinen provisioning bug'ı). Bu btree varken planlayıcı genelde CreatedAt-geriye-tarama +
    /// LIMIT erken-durma planını seçer (Seq Scan değil, kabul edilebilir). Yeni composite index'in
    /// GARANTİLİ kazancı TEK-variant timeline'da (SingleVariant testi) ve CreatedAt yalnızca BRIN
    /// olan prod senaryosunda ortaya çıkar — orada geriye-tarama mümkün olmadığından planlayıcı
    /// ya (ProductVariantId,BranchOfficeId)+Sort ya da yeni composite'i kullanır; composite kazanır.
    /// Bu yüzden burada belirli index adı DEĞİL, "index kullanılıyor / Seq Scan yok" doğrulanır.
    /// </summary>
    [Fact]
    public async Task MultiVariant_Timeline_StaysIndexed_NoSeqScan()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            $"SELECT \"Id\" FROM \"StockMovements\" WHERE NOT \"IsDeleted\" " +
            $"AND \"ProductVariantId\" = ANY(ARRAY['{VariantA}','{VariantB}','{VariantC}']::uuid[]) " +
            $"ORDER BY \"CreatedAt\" DESC LIMIT 100");

        plan.Should().Contain("Index",
            $"Çok-variant'lı ürün timeline'ı index-tabanlı bir plan kullanmalı (composite seek ya da CreatedAt erken-durma). Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"StockMovements\"",
            $"Büyük StockMovements tablosunda ürün timeline'ı asla Seq Scan yapmamalı. Plan:\n{plan}");
    }
}
