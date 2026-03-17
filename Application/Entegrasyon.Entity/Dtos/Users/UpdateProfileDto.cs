using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Users;

public sealed record UpdateProfileDto
{
    [Required, StringLength(60)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(60)] public string Surname { get; init; } = string.Empty;
}
