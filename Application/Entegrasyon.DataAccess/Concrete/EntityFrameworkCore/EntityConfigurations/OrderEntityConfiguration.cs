
using Entegrasyon.Entity.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
        builder.OwnsOne(x => x.ShippingAddress);
        builder.OwnsOne(x => x.BillingAddress);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.TotalQuantity).HasDefaultValue(0);
        builder.Property(x => x.TotalPrice).HasDefaultValue(0m);

        // Hot-path: kategori performans / dashboard / unified-sales sorguları zaman penceresini
        // Orders.OrderDate üzerinden daraltır. OrderDate nullable olduğundan ve sorgular daima
        // !IsDeleted query filter'ı uyguladığından, NULL ve soft-deleted satırları index dışında
        // tutan partial index hem küçük hem seçici kalır.
        // Filter formu HasQueryFilter(!IsDeleted)'ın ürettiği SQL ile birebir aynı
        // ("NOT \"IsDeleted\"") tutulur — böylece PostgreSQL planner'ı partial index'i
        // her sürümde predicate-implication tereddütü olmadan seçer.
        builder.HasIndex(x => x.OrderDate)
            .HasFilter("\"OrderDate\" IS NOT NULL AND NOT \"IsDeleted\"");

        // Hot-path: Sipariş Hazırlama (Picking) KPI + sipariş listesi pazaryeri statüsüne göre
        // filtreler (MarketplaceOrderStatus == "Created" / dinamik @status) ve OrderDate ile
        // bugünkü/24s+ bekleyen kovalarını sayar. Composite (eşitlik kolonu önce, sonra range):
        // status eşitliği prefix'i + OrderDate range/sıralama tek index'le karşılanır.
        // Partial: storefront siparişlerinde MarketplaceOrderStatus NULL'dur (ayrı
        // StorefrontOrderStatus kolonu kullanılır) ve "== 'Created'"/"== @status" NULL'la asla
        // eşleşmez → NULL satırları index dışında bırakmak index'i küçük ve seçici tutar.
        // NOT "IsDeleted" formu query filter'ın ürettiği SQL ile birebir → planner partial'ı seçer.
        builder.HasIndex(x => new { x.MarketplaceOrderStatus, x.OrderDate })
            .HasFilter("\"MarketplaceOrderStatus\" IS NOT NULL AND NOT \"IsDeleted\"");

        // Hot-path: Müşteri raporu RFM segmentasyonu + cohort (tutma) matrisi + dormant listesi.
        // Üç aggregate de storefront B2C siparişlerini müşteri başına gruplar:
        //   - Recency  = MAX(OrderDate)  → son alışveriş (VIP son-30-gün / dormant 90+ gün)
        //   - Monetary = SUM(GrossAmount) → toplam harcama (VIP üst %20 persentil)
        //   - Cohort   = MIN(OrderDate)  → müşterinin ilk-alışveriş ayı + sonraki ay tekrar alışveriş
        // Hepsi GroupBy(CustomerId) + OrderDate üzerinde MIN/MAX/range. Composite'te eşitlik/grup
        // kolonu (CustomerId) önde, zaman kolonu (OrderDate) sonda → GroupBy prefix'i + OrderDate
        // MIN/MAX/range tek index'le karşılanır (index-only'a yakın aggregate).
        // Partial: marketplace siparişlerinde CustomerId NULL'dur (storefront-only B2C müşteri
        // ilişkisi) → NULL satırları index dışında bırakmak hem küçük hem RFM evrenine seçici tutar.
        // NOT "IsDeleted" formu query filter'ın ürettiği SQL ile birebir → planner partial'ı seçer.
        builder.HasIndex(x => new { x.CustomerId, x.OrderDate })
            .HasFilter("\"CustomerId\" IS NOT NULL AND NOT \"IsDeleted\"");
    }
}
