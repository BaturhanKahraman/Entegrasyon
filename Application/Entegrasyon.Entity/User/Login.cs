
namespace Entegrasyon.Entity.User;
public class Login:BaseEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset LoginTime { get; set; }
    public string IpAddress { get; set; }


    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; }
}