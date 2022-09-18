using Shared.Entity;

namespace Entegrasyon.Entity;

public class BranchOffice:ApplicationEntity
{
    public string Name { get; set; }
    public ICollection<ApplicationUser> Users { get; set; }
}