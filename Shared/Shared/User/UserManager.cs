namespace Shared.User;

public class UserManager:IUserManager
{
    public async Task CreateUser(RootUser user)
    {
        return Task.CompletedTask;
    }

    public async Task DeleteUser(RootUser user)
    {

    }
    
}