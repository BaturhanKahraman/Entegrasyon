using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Auth;

public record LoginDto([Required]string UserName,string Password);