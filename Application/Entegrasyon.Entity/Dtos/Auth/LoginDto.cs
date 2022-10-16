using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Auth;

public sealed record LoginDto([Required]string UserName,string Password);