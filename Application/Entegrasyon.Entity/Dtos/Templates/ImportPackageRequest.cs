namespace Entegrasyon.Entity.Dtos.Templates;

/// <summary>
/// Paket import isteği. Çakışma çözüm kararlarını içerir.
/// </summary>
public record ImportPackageRequest
{
    public int PackageId { get; init; }
    public List<ConflictResolution> ConflictResolutions { get; init; } = [];
}

/// <summary>
/// Tek bir çakışma için kullanıcının kararı.
/// </summary>
public record ConflictResolution
{
    public int TemplateEntityId { get; init; }
    public int ExistingEntityId { get; init; }
    public ConflictResolutionStrategy Strategy { get; init; }
}

public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Kullanıcının mevcut entity'sini koru, eksik marketplace mapping'leri ekle.
    /// </summary>
    UseExisting,

    /// <summary>
    /// Template verisini kullan, mevcut entity'yi güncelle.
    /// </summary>
    UseImported
}
