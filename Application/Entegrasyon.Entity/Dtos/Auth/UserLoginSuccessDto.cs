using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Dtos.Auth;

public sealed record UserLoginSuccessDto(
        Guid Id,
        string Name,
        string Surname,
        string Username,
        ICollection<Role> Roles
    );