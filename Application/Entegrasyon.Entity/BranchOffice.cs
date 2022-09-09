using Shared.Entity;

namespace Entegrasyon.Entity;

public class BranchOffice:ApplicationEntity
{
    public string Name { get; set; }
    public List<ApplicationUser> Users { get; set; }
}