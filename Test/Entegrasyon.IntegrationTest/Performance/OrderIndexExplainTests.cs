using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Performance;

/// <summary>
/// PERF HIGH-1 (Task #6) — Orders polling matcher / listeleme index'leri.
///
/// Denetim bulgusu (TDD + pg_indexes ile dogrulandi): Fresh-migrate edilmis bir Orders
/// tablosunda YALNIZCA IX_Orders_MarketPlaceId + IX_Orders_MarketplaceOrderStatus_Active +
/// PK var. Pazaryeri polling matcher'larinin tarif ettigi sutunlar (OrderNumber,
/// ShipmentPackageId, CustomerId) ve admin listeleme (MarketPlaceId+OrderDate sort) index'siz
/// → 50k satirda seq scan / seq scan+sort. (PostgresOptimizations migration'inin urettigi iki
/// index fresh DB'de mevcut DEGIL — multi-tenant provisioning tutarsizligi; bu migration kalici cozer.)
///
/// Bu testler EXPLAIN ile (gercekci 50k satir, gercek planlayici) eklenen partial index'lerin
/// (WHERE NOT IsDeleted) ilgili sorgu sekillerinde KULLANILDIGINI kanitlar. Index'ler eklenmeden
/// ONCE RED (seq scan), sonra GREEN (index scan).
/// </summary>
[Trait("Category", "Integration")]
public class OrderIndexExplainTests : PerfIndexTestBase
{
    public OrderIndexExplainTests(PostgreSqlFixture pg) : base(pg) { }

    protected override string TableToAnalyze => "Orders";

    protected override async Task SeedAsync(IntegrationDbContext db)
    {
        // Trendyol (Id=1) InitialCreate migration ile seed'li → MarketPlaceId FK gecerli.
        // Gercekci olcek (50k satir) raw bulk insert ile (EF tek tek insert'ten cok hizli).
        // Bu olcekte secici esitlik/sirali index, seq scan'den belirgin ucuzdur → gercek
        // planlayici dogru index'i kendiliginden secer (seqscan hack'i gerekmez).
        const string sql = """
            INSERT INTO "Orders"
                ("Id","CreatedAt","UpdatedAt","DeletedAt","IsDeleted",
                 "OrderNumber","ShipmentPackageId","CustomerId","MarketPlaceId",
                 "OrderDate","MarketplaceOrderStatus","IsMicro","IsFastDelivery","IsGiftWrapped","HideInvoice")
            SELECT
                gen_random_uuid(), now(), now(), now(), false,
                'ORD-' || lpad(g::text, 6, '0'),
                100000 + g,
                g + 1,
                1,
                now() - (g || ' minutes')::interval,
                CASE WHEN g % 2 = 0 THEN 'Created' ELSE 'Shipped' END,
                false, false, false, false
            FROM generate_series(0, 49999) AS g;
            """;
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    [Fact]
    public async Task OrderNumber_Lookup_Uses_Index()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            "SELECT \"Id\" FROM \"Orders\" WHERE NOT \"IsDeleted\" AND \"OrderNumber\" = ANY(ARRAY['ORD-000050','ORD-000051'])");

        plan.Should().Contain("IX_Orders_OrderNumber_Active",
            $"OrderNumber matcher (N11/Pazarama OrderManager:172/315 + GetOrderByNumber:697) index kullanmali. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"Orders\"");
    }

    [Fact]
    public async Task CustomerId_Lookup_Uses_Index()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            "SELECT \"Id\" FROM \"Orders\" WHERE NOT \"IsDeleted\" AND \"CustomerId\" = 7");

        plan.Should().Contain("IX_Orders_CustomerId_Active",
            $"Storefront 'siparislerim' (OrderManager:631) index kullanmali. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"Orders\"");
    }

    [Fact]
    public async Task ShipmentPackageId_Lookup_Uses_Index()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            "SELECT \"Id\" FROM \"Orders\" WHERE NOT \"IsDeleted\" AND \"ShipmentPackageId\" = ANY(ARRAY[100050,100051]::bigint[])");

        plan.Should().Contain("IX_Orders_ShipmentPackageId_Active",
            $"Trendyol ShipmentPackageId matcher (OrderManager:450) index kullanmali. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"Orders\"");
    }

    [Fact]
    public async Task MarketPlace_OrderDate_Listing_Uses_Composite_Index()
    {
        await using var db = CreateDbContext();
        var plan = await ExplainHelper.ExplainAsync(db,
            "SELECT \"Id\" FROM \"Orders\" WHERE NOT \"IsDeleted\" AND \"MarketPlaceId\" = 1 ORDER BY \"OrderDate\" DESC LIMIT 50");

        plan.Should().Contain("IX_Orders_MarketPlaceId_OrderDate_Active",
            $"Admin siparis listesi filtre+sort (OrderManager:50-60) composite index kullanmali. Plan:\n{plan}");
        plan.Should().NotContain("Seq Scan on \"Orders\"");
    }
}
