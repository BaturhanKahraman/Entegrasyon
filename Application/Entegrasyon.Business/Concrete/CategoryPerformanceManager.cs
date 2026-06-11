using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Kategori detay sayfası satış/performans aggregate'i.
///
/// Hot-path tasarım notları:
/// - Filtre tabanı: OrderItem → ProductVariant (Product nav) → MainProduct (CategoryId)
///   + Order.OrderDate >= cutoff. Tüm filtreler tek bir taban IQueryable'da kurulur.
/// - 3 sabit aggregate sorgusu (toplamlar / top5 ürün / marketplace breakdown). N+1 YOK;
///   her biri sunucu tarafında GroupBy ile çalışır, satır setini belleğe çekmez.
/// - Hepsi AsNoTracking (DbContext default no-tracking) + skaler projeksiyon (full-entity çekmez).
/// - Multi-tenant: IDbContextFactory ile tenant'a özgü DbContext (DB-per-tenant). Connection
///   tenant'a göre değişir; query gövdesi tüm tenant'larda aynı → compiled-query güvenli.
/// - İndex bağımlılığı (DB Master garantisi): Orders.OrderDate (zaman penceresi),
///   OrderItems.ProductId + OrderItems.OrderId (FK), ProductVariants.ProductId (FK),
///   MainProducts.CategoryId (FK), Orders.MarketPlaceId (FK).
/// </summary>
public sealed class CategoryPerformanceManager(IDbContextFactory<IntegrationDbContext> contextFactory)
    : ICategoryPerformanceManager
{
    public async Task<CategoryPerformanceDto> GetCategoryPerformanceAsync(
        int categoryId,
        int daysPast = 30,
        CancellationToken ct = default)
    {
        if (daysPast < 1)
        {
            daysPast = 1;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-daysPast);

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        // Taban: bu kategoriye DOĞRUDAN bağlı ürünlerin (MainProduct.CategoryId == categoryId)
        // zaman penceresi içindeki sipariş satırları. Query filter (!IsDeleted) Order/OrderItem/
        // ProductVariant/Product config'lerinde tanımlı → otomatik uygulanır.
        //
        // GroupBy'dan ÖNCE satır-başı türetilmiş değerleri (NetQty, LineRevenue) düz skaler
        // kolonlara projekte ederiz. Böylece grup-içi aggregate'ler basit Sum(x => x.Kolon)
        // haline gelir ve Npgsql tarafından sorunsuz translate edilir (navigation üzerinden
        // çarpım/çıkarma içeren grup aggregate'i bazı durumlarda translate edilemez).
        var lines = dbContext.OrderItems
            .Where(oi => oi.Product!.Product.CategoryId == categoryId)
            .Where(oi => oi.Order.OrderDate != null && oi.Order.OrderDate >= cutoff)
            .Select(oi => new
            {
                oi.OrderId,
                MainProductId = oi.Product!.ProductId,
                ProductTitle = oi.Product.Product.Title,
                oi.Order.MarketPlaceId,
                MarketPlaceName = oi.Order.MarketPlace!.Name,
                oi.Quantity,
                oi.ReturnedQuantity,
                NetQty = oi.Quantity - oi.ReturnedQuantity,
                // Çarpımı decimal-decimal olarak sabitle: int Quantity'yi decimal'e projekte
                // ederek EF'in grup aggregate pushdown'ında "numeric * (decimal)int" cast'i
                // üretmesini ve çok-katmanlı join'li grupta translate edememesini önleriz.
                LineRevenue = oi.UnitPrice * (decimal)oi.Quantity
            });

        // 1) Genel toplamlar — tek aggregate satırı.
        var totals = await lines
            .GroupBy(_ => 1)
            .Select(g => new
            {
                SoldQuantity = g.Sum(x => x.NetQty),
                ReturnedQuantity = g.Sum(x => x.ReturnedQuantity),
                GrossQuantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineRevenue),
                LineCount = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        if (totals is null || totals.LineCount == 0)
        {
            // Satış yok → tüm değerler 0, listeler boş (asla null).
            return new CategoryPerformanceDto(
                CategoryId: categoryId,
                DaysPast: daysPast,
                TotalSoldQuantity: 0,
                TotalRevenue: 0m,
                TotalOrderCount: 0,
                TotalReturnedQuantity: 0,
                ReturnRate: 0m,
                AverageUnitPrice: 0m,
                TopProducts: [],
                MarketplaceBreakdown: []);
        }

        // DISTINCT sipariş sayısı — ayrı skaler sorgu (indexli OrderId ile ucuz).
        var totalOrderCount = await lines
            .Select(x => x.OrderId)
            .Distinct()
            .CountAsync(ct);

        // 2) En çok ciro yapan ilk 5 ürün (MainProduct bazında).
        // GroupBy + Sum sunucuda; OrderByDescending(Revenue).Take(5) bellekte. Npgsql,
        // "GROUP BY ... ORDER BY <aggregate ifadesi> LIMIT" kombinasyonunda karmaşık
        // aggregate'i (UnitPrice * Quantity) translate edemiyor. Grup sayısı kategorideki
        // ürün adedi kadar (sınırlı) olduğundan bellekte sıralama+Take ucuz ve satır
        // patlaması yaratmaz. Aggregate'ler yine sunucuda hesaplanır.
        var topProductGroups = await lines
            .GroupBy(x => new { x.MainProductId, x.ProductTitle })
            .Select(g => new CategoryTopProductDto(
                g.Key.MainProductId,
                g.Key.ProductTitle,
                g.Sum(x => x.NetQty),
                g.Sum(x => x.LineRevenue)))
            .ToListAsync(ct);

        var topProducts = topProductGroups
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

        // 3) Pazaryeri kırılımı — MarketPlaceId bazında. Null → "Direkt/Storefront".
        // OrderBy(Revenue) bellekte (bkz. topProducts notu). Marketplace adedi küçük.
        var marketplaceRaw = await lines
            .GroupBy(x => new { x.MarketPlaceId, x.MarketPlaceName })
            .Select(g => new
            {
                g.Key.MarketPlaceId,
                Name = g.Key.MarketPlaceName,
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                Revenue = g.Sum(x => x.LineRevenue)
            })
            .ToListAsync(ct);

        var marketplaceBreakdown = marketplaceRaw
            .OrderByDescending(m => m.Revenue)
            .Select(m => new CategoryMarketplaceBreakdownDto(
                m.MarketPlaceId,
                m.MarketPlaceId == null ? "Direkt/Storefront" : (m.Name ?? "Bilinmeyen"),
                m.OrderCount,
                m.Revenue))
            .ToList();

        var returnRate = totals.GrossQuantity > 0
            ? Math.Round((decimal)totals.ReturnedQuantity / totals.GrossQuantity * 100m, 2)
            : 0m;

        // Ağırlıklı ortalama efektif birim fiyat = toplam ciro / brüt satılan adet.
        // Satır-ağırlıksız (UnitPriceSum/LineCount) ortalama yanıltıcı olur (3 adetlik satır
        // ile 1 adetlik satırı eşit ağırlıklar); analitik olarak doğru metrik ciro/adet.
        var averageUnitPrice = totals.GrossQuantity > 0
            ? Math.Round(totals.Revenue / totals.GrossQuantity, 2)
            : 0m;

        return new CategoryPerformanceDto(
            CategoryId: categoryId,
            DaysPast: daysPast,
            TotalSoldQuantity: totals.SoldQuantity,
            TotalRevenue: totals.Revenue,
            TotalOrderCount: totalOrderCount,
            TotalReturnedQuantity: totals.ReturnedQuantity,
            ReturnRate: returnRate,
            AverageUnitPrice: averageUnitPrice,
            TopProducts: topProducts,
            MarketplaceBreakdown: marketplaceBreakdown);
    }
}
