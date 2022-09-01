
namespace Shared.User.Token;

public class RootJwtToken
{
    public int Id { get; set; }
    public string JwtToken { get; set; }
    public bool CurrentlyUsing { get; set; }
    public Device Device { get; set; }
    public Guid ApplicationUserId { get; set; }
    public RootUser User { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}