using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Business.Concrete;

public sealed class VariantNamingService : IVariantNamingService
{
    public string? Compute(ProductVariant variant, Product product)
    {
        var values = variant.ProductVariantAttributes
            .Where(a => a.IsVarianter || a.IsSlicer)
            .OrderByDescending(a => a.IsVarianter)
            .Select(a => a.CategoryAttributeValue ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v));

        var computed = string.Join(" ", values).Trim();

        if (!string.IsNullOrWhiteSpace(computed)) return computed;
        if (!string.IsNullOrWhiteSpace(product.Title)) return product.Title;
        return null;
    }
}
