using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Ürün detay sayfası satış/performans aggregate'i — CategoryPerformanceManager deseninin
/// MainProduct (productId) bazına uyarlanmış hali.
///
/// Hot-path tasarım notları:
/// - Filtre tabanı: OrderItem → ProductVariant (oi.Product) — bu MainProduct'a bağlı TÜM
///   varyantların (ProductVariant.ProductId == productId) zaman penceresi içindeki satırları.
/// - 3 sabit aggregate sorgusu (toplamlar / varyant kırılımı / marketplace breakdown). N+1 YOK;
///   her biri sunucu tarafında GroupBy ile çalışır, satır setini belleğe çekmez.
/// - Hepsi AsNoTracking (DbContext default no-tracking) + skaler projeksiyon (full-entity çekmez).
/// - Multi-tenant: IDbContextFactory ile tenant'a özgü DbContext (DB-per-tenant). Connection
///   tenant'a göre değişir; query gövdesi tüm tenant'larda aynı → compiled-query güvenli.
/// - İndex bağımlılığı (DB Master garantisi): ProductVariants.ProductId (explicit, hot filtre),
///   Orders.OrderDate (zaman penceresi), OrderItems.ProductId + OrderItems.OrderId (FK),
///   Orders.MarketPlaceId (FK). Ek migration GEREKMEZ — hepsi mevcut.
/// </summary>
public sealed class ProductPerformanceManager(IDbContextFactory<IntegrationDbContext> contextFactory)
    : IProductPerformanceManager
{
    public async Task<ProductPerformanceDetailDto> GetProductPerformanceAsync(
        Guid productId,
        int daysPast = 30,
        CancellationToken ct = default)
    {
        if (daysPast < 1)
        {
            daysPast = 1;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-daysPast);

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        // Taban: bu MainProduct'a bağlı varyantların (ProductVariant.ProductId == productId)
        // zaman penceresi içindeki sipariş satırları. Query filter (!IsDeleted) Order/OrderItem/
        // ProductVariant config'lerinde tanımlı → otomatik uygulanır.
        //
        // GroupBy'dan ÖNCE satır-başı türetilmiş değerleri (NetQty, LineRevenue) düz skaler
        // kolonlara projekte ederiz; böylece grup-içi aggregate'ler basit Sum(x => x.Kolon)
        // haline gelir ve Npgsql tarafından sorunsuz translate edilir.
        var lines = dbContext.OrderItems
            .Where(oi => oi.Product!.ProductId == productId)
            .Where(oi => oi.Order.OrderDate != null && oi.Order.OrderDate >= cutoff)
            .Select(oi => new
            {
                oi.OrderId,
                VariantId = oi.Product!.Id,
                VariantName = oi.Product.Name,
                VariantBarcode = oi.Product.Barcode,
                oi.Order.MarketPlaceId,
                MarketPlaceName = oi.Order.MarketPlace!.Name,
                oi.Quantity,
                oi.ReturnedQuantity,
                NetQty = oi.Quantity - oi.ReturnedQuantity,
                // Çarpımı decimal-decimal olarak sabitle (int Quantity'yi decimal'e projekte ederek)
                // — çok-katmanlı join'li grup aggregate translate sorununu önler.
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
            return new ProductPerformanceDetailDto(
                ProductId: productId,
                DaysPast: daysPast,
                TotalSoldQuantity: 0,
                TotalRevenue: 0m,
                TotalOrderCount: 0,
                TotalReturnedQuantity: 0,
                ReturnRate: 0m,
                AverageUnitPrice: 0m,
                VariantBreakdown: [],
                MarketplaceBreakdown: []);
        }

        // DISTINCT sipariş sayısı — ayrı skaler sorgu (indexli OrderId ile ucuz).
        var totalOrderCount = await lines
            .Select(x => x.OrderId)
            .Distinct()
            .CountAsync(ct);

        // 2) Varyant kırılımı (renk/beden vb.) — ciroya göre azalan.
        // GroupBy + Sum sunucuda; OrderByDescending(Revenue) bellekte. Npgsql, karmaşık
        // aggregate'in (UnitPrice * Quantity) "GROUP BY ... ORDER BY <aggregate> LIMIT"
        // kombinasyonunu translate edemiyor; varyant adedi ürün başına sınırlı (renk×beden)
        // olduğundan bellekte sıralama ucuz ve satır patlaması yaratmaz.
        var variantGroups = await lines
            .GroupBy(x => new { x.VariantId, x.VariantName, x.VariantBarcode })
            .Select(g => new
            {
                g.Key.VariantId,
                g.Key.VariantName,
                g.Key.VariantBarcode,
                SoldQuantity = g.Sum(x => x.NetQty),
                Revenue = g.Sum(x => x.LineRevenue)
            })
            .ToListAsync(ct);

        var variantBreakdown = variantGroups
            .OrderByDescending(v => v.Revenue)
            .Select(v => new ProductVariantPerformanceDto(
                v.VariantId,
                // İsim önceliği: Name → Barcode → "Varyant" (asla null/boş etiket).
                !string.IsNullOrWhiteSpace(v.VariantName) ? v.VariantName
                    : !string.IsNullOrWhiteSpace(v.VariantBarcode) ? v.VariantBarcode
                    : "Varyant",
                v.VariantBarcode,
                v.SoldQuantity,
                v.Revenue))
            .ToList();

        // 3) Pazaryeri kırılımı — MarketPlaceId bazında. Null → "Direkt/Storefront".
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
            .Select(m => new ProductMarketplaceBreakdownDto(
                m.MarketPlaceId,
                m.MarketPlaceId == null ? "Direkt/Storefront" : (m.Name ?? "Bilinmeyen"),
                m.OrderCount,
                m.Revenue))
            .ToList();

        var returnRate = totals.GrossQuantity > 0
            ? Math.Round((decimal)totals.ReturnedQuantity / totals.GrossQuantity * 100m, 2)
            : 0m;

        // Ağırlıklı ortalama efektif birim fiyat = toplam ciro / brüt satılan adet.
        var averageUnitPrice = totals.GrossQuantity > 0
            ? Math.Round(totals.Revenue / totals.GrossQuantity, 2)
            : 0m;

        return new ProductPerformanceDetailDto(
            ProductId: productId,
            DaysPast: daysPast,
            TotalSoldQuantity: totals.SoldQuantity,
            TotalRevenue: totals.Revenue,
            TotalOrderCount: totalOrderCount,
            TotalReturnedQuantity: totals.ReturnedQuantity,
            ReturnRate: returnRate,
            AverageUnitPrice: averageUnitPrice,
            VariantBreakdown: variantBreakdown,
            MarketplaceBreakdown: marketplaceBreakdown);
    }
}
