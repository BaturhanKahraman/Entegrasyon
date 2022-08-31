using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Users;

public class ApplicationLogin : LongEntity
{
    public DateTimeOffset LoginTime { get; set; }
    public string IPAddress { get; set; }


    public Guid UserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
}