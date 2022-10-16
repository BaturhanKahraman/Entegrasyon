using Shared.User;

namespace Entegrasyon.Entity;

public sealed class ApplicationUser:RootUser
{
    public int? DefaultBranchOfficeId { get; set; }
    public BranchOffice DefaultBranchOffice { get; set; }
}