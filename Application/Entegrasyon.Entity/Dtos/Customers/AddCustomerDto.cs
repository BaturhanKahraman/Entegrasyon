namespace Entegrasyon.Entity.Dtos.Customers;

public sealed record AddCustomerDto(
    string NationalIdentityOrTaxNumber,
    string Name,
    string Surname,
    string CorporateName,
    string PhoneNumber,
    string FullAddress,
    string CustomerType);