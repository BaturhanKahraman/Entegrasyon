using Shared.Entity;

namespace Shared.User;

public class RootRole : ApplicationEntity
{
    public string Name { get; set; }
    public List<RootUser> Users { get; set; }
    public List<RootClaim> Claims { get; set; }
}