using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Users;

public sealed record ChangePasswordDto
{
    [Required] public string CurrentPassword { get; init; } = string.Empty;
    [Required, MinLength(4), MaxLength(255)] public string NewPassword { get; init; } = string.Empty;
    [Required] public string ConfirmPassword { get; init; } = string.Empty;
}
