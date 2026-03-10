using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity;

public sealed class BranchOffice : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDefaultMarketPlaceStock { get; set; }
    public IEnumerable<ApplicationUser> Users { get; set; }
}