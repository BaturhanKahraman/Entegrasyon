using System.Diagnostics;

namespace Entegrasyon.Business.Diagnostics;

/// <summary>
/// Merkezi ActivitySource — tüm custom trace'ler buradan oluşturulur.
/// Program.cs'te .AddSource(EntegrasyonActivitySource.Name) ile kayıt edilmeli.
/// </summary>
public static class EntegrasyonActivitySource
{
    public const string Name = "Entegrasyon.Business";
    public static readonly ActivitySource Instance = new(Name);

    // Marketplace operasyonları için genel trace
    public static Activity? StartMarketplaceOperation(string marketplace, string operation)
        => Instance.StartActivity($"{marketplace}.{operation}", ActivityKind.Internal)?
            .SetTag("marketplace.name", marketplace)
            .SetTag("marketplace.operation", operation);

    // Ürün senkronizasyonu — productId ile eşleştirme kolaylaşır
    public static Activity? StartProductSync(string marketplace, Guid productId)
        => Instance.StartActivity($"{marketplace}.ProductSync", ActivityKind.Internal)?
            .SetTag("marketplace.name", marketplace)
            .SetTag("product.id", productId.ToString());

    // Mapping doğrulama adımı
    public static Activity? StartValidation(string marketplace)
        => Instance.StartActivity($"{marketplace}.Validation", ActivityKind.Internal)?
            .SetTag("marketplace.name", marketplace);

    // DTO → marketplace payload dönüşüm adımı
    public static Activity? StartMapping(string marketplace)
        => Instance.StartActivity($"{marketplace}.Mapping", ActivityKind.Internal)?
            .SetTag("marketplace.name", marketplace);

    // Toplu işlem (sync-all, retry-all)
    public static Activity? StartBulkOperation(string operation, int itemCount)
        => Instance.StartActivity($"Bulk.{operation}", ActivityKind.Internal)?
            .SetTag("bulk.operation", operation)
            .SetTag("bulk.item_count", itemCount);
}
