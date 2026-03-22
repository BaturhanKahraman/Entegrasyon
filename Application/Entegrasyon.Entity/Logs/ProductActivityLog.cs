using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Logs;

/// <summary>
/// Ürün bazlı kronolojik aktivite logu.
/// Kullanıcı ürün detayına girdiğinde "bu ürüne ne olmuş" timeline'ını görür.
/// </summary>
public sealed class ProductActivityLog : BaseEntity
{
    public long Id { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public ProductActivityType ActivityType { get; set; }

    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Detail { get; set; }

    public ProductActivityStatus Status { get; set; }

    /// <summary>
    /// İlgili marketplace adı (nullable — marketplace-dışı aktiviteler için).
    /// </summary>
    [StringLength(50)]
    public string? MarketplaceName { get; set; }

    /// <summary>
    /// İlgili batch ID veya referans numarası.
    /// </summary>
    [StringLength(200)]
    public string? ReferenceId { get; set; }
}

public enum ProductActivityType
{
    Created = 1,
    Updated,
    MappingValidated,
    PublishRequested,
    PublishSent,
    BatchCompleted,
    BatchFailed,
    Approved,
    Rejected,
    Archived,
    StockUpdated,
    PriceUpdated,
    ContentUpdated,
    ImageUpdated,
    Deleted
}

public enum ProductActivityStatus
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}
