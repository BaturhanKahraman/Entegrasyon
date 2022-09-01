using Shared.Entity;

namespace Shared.User;

public class RootLogin : LongEntity
{
    public DateTimeOffset LoginTime { get; set; }
    public string IpAddress { get; set; }


    public Guid RootUserId { get; set; }
    public RootUser RootUser { get; set; }
}