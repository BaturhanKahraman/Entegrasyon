namespace Entegrasyon.Entity.Dtos.Product.Activity;

/// <summary>
/// Ürün 360° "Siparişler" sekmesi satırı — bu ürünü (variant'ları üzerinden) içeren siparişler.
/// </summary>
public sealed record ProductOrderReferenceDto(
    Guid OrderId,
    string? OrderNumber,
    DateTimeOffset OrderDate,
    string Platform,
    string? VariantLabel,
    int Quantity);
