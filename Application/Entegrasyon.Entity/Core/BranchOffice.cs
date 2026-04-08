using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity;

public sealed class BranchOffice : BaseEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// NormalizedName, BranchNameNormalizer.Normalize ile ayarlanır.
    /// Filtered unique index: WHERE IsDeleted = false. Silinen ofislerin ismi yeniden kullanılabilir.
    /// </summary>
    public string? NormalizedName { get; set; }

    /// <summary>Opsiyonel adres, tek text field.</summary>
    public string? Address { get; set; }

    /// <summary>
    /// Merkez ofis (HQ) flag'i. Seed row'u Id=1 olarak ayarlanır.
    /// HQ silinemez ve silme talebine konu olamaz. Name/Address serbestçe düzenlenebilir.
    /// </summary>
    public bool IsHeadquarters { get; set; }

    public bool IsDefaultMarketPlaceStock { get; set; }

    /// <summary>
    /// Denormalized cache: aktif Pending bir silme talebi var mı? O(1) sorgu için.
    /// Invariant: (!IsDeleted && DeletionRequestId != null) ⇔ Pending bir talep var.
    /// Reject → null, Approve → null (soft-delete anında), talep aç → request.Id.
    /// </summary>
    public int? DeletionRequestId { get; set; }
    public BranchOfficeDeletionRequest? DeletionRequest { get; set; }

    public IEnumerable<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}