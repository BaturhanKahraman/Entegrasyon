namespace Entegrasyon.Entity.Dtos.CargoCompany;

public sealed record AddCargoCompanyDto
{
    
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string TaxNumber { get; set; } = null!;
}