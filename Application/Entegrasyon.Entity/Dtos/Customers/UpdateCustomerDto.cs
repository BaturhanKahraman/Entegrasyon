namespace Entegrasyon.Entity.Dtos.Customers;

public sealed record UpdateCustomerDto(int Id,string NationalIdentityOrTaxNumber,string NameOrCorporateName,string Surname);