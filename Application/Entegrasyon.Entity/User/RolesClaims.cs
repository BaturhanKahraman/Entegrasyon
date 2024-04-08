using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.User;
/// <summary>
/// Many to many table for role and application claim
/// </summary>
public class RolesClaims
{
    public int RoleId { get; set; }
    public Role Role { get; set; }
    public int ApplicationClaimId { get; set; }
    public ApplicationClaim ApplicationClaim { get; set; }

}