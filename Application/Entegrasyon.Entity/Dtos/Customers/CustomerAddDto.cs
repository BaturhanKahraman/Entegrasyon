namespace Entegrasyon.Entity.Dtos.Customers;

public sealed record CustomerAddDto(
    string NationalIdentity,
    string TaxNumber,
    string Name,
    string Surname,
    string CorporateName,
    string PhoneNumber,
    string FullAddress,
    string CustomerType);