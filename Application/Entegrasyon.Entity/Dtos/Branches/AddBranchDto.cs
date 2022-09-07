using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Dtos.Branches;

public record AddBranchDto
{
    public string Name { get; init; }
}