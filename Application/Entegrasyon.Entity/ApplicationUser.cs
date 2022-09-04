using Shared.User;

namespace Entegrasyon.Entity;

public class ApplicationUser:RootUser
{
    public int? DefaultBranchOfficeId { get; set; }
    public virtual BranchOffice DefaultBranchOffice { get; set; }
}