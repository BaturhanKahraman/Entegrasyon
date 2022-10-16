namespace Entegrasyon.Entity.Dtos.Customers;

public sealed record AddCustomerDto(string NationalIdentity,string Name,string Surname,string PhoneNumber,string Address);