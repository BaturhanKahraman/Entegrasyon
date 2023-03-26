namespace Entegrasyon.Entity.Dtos.Customers;

public sealed record UpdateCustomerDto(
    int Id,
     string PhoneNumber,
     string CustomerType,
     string Name,
     string Surname,
    string NationalIdentity,
    string TaxNumber,
    string CorporateName
);