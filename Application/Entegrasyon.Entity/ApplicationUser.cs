using Shared.User;

namespace Entegrasyon.Entity;

public class ApplicationUser:RootUser
{
    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; }
}