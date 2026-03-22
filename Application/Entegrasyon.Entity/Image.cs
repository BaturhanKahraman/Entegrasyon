﻿using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity;

public sealed class Image : BaseEntity
{
    public int Id { get; set; }

    // Relations
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    // Storage
    [StringLength(500)]
    public string? StorageKey { get; set; }  // "tenants/1/products/42/variants/guid/originals/abc123.jpg"

    public FileStorageType FileStorageType { get; set; }

    // Metadata (calculated on upload)
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
    public long FileSizeBytes { get; set; }

    [StringLength(50)]
    public string? ContentType { get; set; }  // "image/webp", "image/jpeg"

    // Display
    public int DisplayOrder { get; set; }  // 0 = main image
    public bool IsMain { get; set; }

    [MaxLength(100)]
    public string? AlternativeText { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    // Processing flags
    public bool ThumbnailGenerated { get; set; }
    public bool MediumGenerated { get; set; }
    public bool LargeGenerated { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Legacy field for backward compatibility
    [StringLength(450)]
    [DataType("varchar")]
    public string? Src { get; set; }

    // Helper to get resized image path
    // public string GetResizedStorageKey(ImageSize size) => size switch
    // {
    //     ImageSize.Original => StorageKey,
    //     ImageSize.Thumb => StorageKey.Replace("/originals/", "/cache/").Replace(Path.GetExtension(StorageKey), "_thumb.webp"),
    //     ImageSize.Medium => StorageKey.Replace("/originals/", "/cache/").Replace(Path.GetExtension(StorageKey), "_medium.webp"),
    //     ImageSize.Large => StorageKey.Replace("/originals/", "/cache/").Replace(Path.GetExtension(StorageKey), "_large.webp"),
    //     _ => StorageKey
    // };
}
