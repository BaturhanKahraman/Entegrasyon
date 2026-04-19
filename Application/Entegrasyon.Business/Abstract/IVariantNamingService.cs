using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Abstract;

public interface IVariantNamingService
{
    // Variant icin Name hesaplar. Varianter ve slicer attribute'larini kullanir;
    // hicbir varianter/slicer deger yoksa product.Title doner; product.Title da bossa null.
    // Sonuc DB'ye yazilir (nullable).
    string? Compute(ProductVariant variant, Product product);
}

public readonly record struct VariantAttributeLite(
    string? Value,
    string? CustomValue,
    bool IsVarianter,
    bool IsSlicer,
    int Order);
