using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity;

/// <summary>
/// Junction tablo: Kullanıcının atanmış olduğu şube ofisleri (many-to-many).
/// Composite PK (UserId, BranchOfficeId). Soft-delete yok — kaldırma = satır silme.
/// ApplicationUser.DefaultBranchOfficeId ise kullanıcının "birincil/atanmış" ofisini tutar
/// ve bu junction satırlarının bir alt kümesidir.
/// </summary>
public sealed class UserBranchOffice
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; } = null!;

    public DateTimeOffset AssignedAt { get; set; }
}
