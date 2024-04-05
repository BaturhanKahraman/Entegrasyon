using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Entegrasyon.Business.Notifications.SignalR;

public class ApplicationUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return connection.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)!.Value;
    }
}

