namespace Entegrasyon.Entity.Dtos.Category;

public sealed record CategorySelectDto(int Id, string Name, string? ParentName = null);
