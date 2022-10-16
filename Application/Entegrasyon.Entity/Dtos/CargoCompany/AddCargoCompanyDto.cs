namespace Entegrasyon.Entity.Dtos.CargoCompany;

public sealed record AddCargoCompanyDto
{
    
    public string Name { get; set; }
    public string Code { get; set; }
    public string TaxNumber { get; set; }
}