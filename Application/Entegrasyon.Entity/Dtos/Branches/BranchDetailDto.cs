namespace Entegrasyon.Entity.Dtos.Branches;

public class BranchDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int UserCount { get; set; }

}