
using System.Text.Json;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductVariantEntityConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Barcode).IsUnique();

        // ProductId'ye partial index — Postgres FK'ye otomatik index AÇMAZ. Ürünler liste sayfasının
        // stok/varyant aggregate'i ve KPI sorgusu pv."ProductId" üzerinden join+group yapıyor; indexsiz
        // bu hot tabloda seq scan olurdu. Partial (WHERE NOT IsDeleted): tüm sorgular silinmemişi
        // filtrelediği için index hem küçük hem hızlı.
        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_ProductVariants_ProductId_Active")
            .HasFilter("\"IsDeleted\" = false");
        builder.Property(x => x.Name).HasMaxLength(256);
        //builder.OwnsMany(x => x.ProductVariantAttributes, navBuilder =>
        //{
        //    navBuilder.ToJson();
        //});
        //builder.Property(x => x.ProductVariantAttributes).HasColumnType("jsonb").HasConversion(
        //    x => JsonSerializer.Serialize(x,new JsonSerializerOptions { WriteIndented = true }),
        //    x => JsonSerializer.Deserialize<List<ProductVariantAttribute>>(x,new JsonSerializerOptions { WriteIndented = true }));
        builder.OwnsMany(x => x.ProductVariantAttributes, ownedNavigationBuilder =>
        {
            ownedNavigationBuilder.ToTable("ProductVariantAttributes");
        });
    }
}