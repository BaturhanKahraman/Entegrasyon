using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.User;

public class UsersClaims
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public int ApplicationClaimId { get; set; }
    public ApplicationClaim ApplicationClaim { get; set; }
}