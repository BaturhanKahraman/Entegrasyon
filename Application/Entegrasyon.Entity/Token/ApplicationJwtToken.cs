using Entegrasyon.Entity.Users;

namespace Entegrasyon.Entity.Token;

public class ApplicationJwtToken
{
    public int Id { get; set; }
    public string JwtToken { get; set; }
    public bool CurrentlyUsing { get; set; }
    public Device Device { get; set; }
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
}