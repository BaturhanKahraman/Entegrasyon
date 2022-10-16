using Shared.Entity;

namespace Entegrasyon.Entity;

public sealed class BranchOffice : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<ApplicationUser> Users { get; set; }
}