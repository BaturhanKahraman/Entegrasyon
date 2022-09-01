using Microsoft.EntityFrameworkCore;
using Shared.User.Token;

namespace Shared.User;

public class UserContext<T>:DbContext
where T:RootUser,new()
{
    public RootClaim Claims { get; set; }
    public RootLogin Logins { get; set; }
    public RootRole Roles { get; set; }
    public RootJwtToken Tokens { get; set; }
    public T Users { get; set; }
}