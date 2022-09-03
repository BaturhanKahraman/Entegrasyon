using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Auth;

public record AssignFirstPasswordDto([Required(ErrorMessage = "Lütfen şifreyi girin")]string Password,[Required] string UserId);