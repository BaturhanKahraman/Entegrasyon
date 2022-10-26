namespace Entegrasyon.Entity.Dtos.Customers;

public sealed class CustomerDetailDto
{
    public CustomerDetailDto()
    {

    }
    public CustomerDetailDto(DateTimeOffset createdAt,int id,string nationalIdentityOrTaxNumber,string nameSurnameOrCorporateName,int salesCount,string phoneNumber,string fullAddress,string customerType)
    {
        CreatedAt = createdAt;
        Id = id;
        NationalIdentityOrTaxNumber = nationalIdentityOrTaxNumber;
        NameSurnameOrCorporateName = nameSurnameOrCorporateName;
        SalesCount = salesCount;
        PhoneNumber = phoneNumber;
        Address = fullAddress;
        CustomerType = customerType;
    }

    public string CustomerType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Id { get; init; }
    public string NationalIdentityOrTaxNumber { get; init; }
    public string NameSurnameOrCorporateName { get; init; }
    public int SalesCount { get; init; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }

}