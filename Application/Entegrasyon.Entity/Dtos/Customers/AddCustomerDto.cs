namespace Entegrasyon.Entity.Dtos.Customers;

public sealed record AddCustomerDto(string NationalIdentityOrTaxNumber,string NameOrCorporateName,string Surname,string PhoneNumber,string FullAddress,string Type);