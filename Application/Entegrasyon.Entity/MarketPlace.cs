using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity;

public sealed class MarketPlace : BaseEntity
{
    public int Id { get; set; }
    [Required, StringLength(maximumLength: 50)]
    public string Name { get; set; } = null!;

    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }

    public bool IsBasicAuth { get; set; } = false;
    public string? BasicAuthUserName { get; set; }
    public string? BasicAuthPassword { get; set; }

    /// <summary>
    /// Trendyol satıcı ID'si — tüm API URL'lerinde {sellerId} olarak kullanılır.
    /// </summary>
    [StringLength(maximumLength: 50)]
    public string? SellerId { get; set; }

    /// <summary>
    /// API base URL — STAGE: https://stageapigw.trendyol.com, PROD: https://apigw.trendyol.com
    /// </summary>
    [StringLength(maximumLength: 200)]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// User-Agent prefix — Trendyol "{SellerId} - SelfIntegration" formatını zorunlu tutar.
    /// </summary>
    [StringLength(maximumLength: 100)]
    public string? UserAgentPrefix { get; set; }
}