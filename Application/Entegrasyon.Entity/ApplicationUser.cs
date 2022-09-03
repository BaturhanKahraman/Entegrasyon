using Shared.User;

namespace Entegrasyon.Entity;

public class ApplicationUser:RootUser
{
    public int? DefaultBranchOfficeId { get; set; }
    public BranchOffice DefaultBranchOffice { get; set; }
}