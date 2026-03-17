namespace Entegrasyon.Entity.Dtos.Customers;

public sealed class CustomerDetailDto
{
    public CustomerDetailDto()
    {

    }//
    public CustomerDetailDto(DateTimeOffset createdAt,int id,string nationalIdentityOrTaxNumber,string nameSurname,string corporateName,int salesCount,string phoneNumber,string fullAddress,string customerType)
    {
        CreatedAt = createdAt;
        Id = id;
        NationalIdentityOrTaxNumber = nationalIdentityOrTaxNumber;
        SalesCount = salesCount;
        PhoneNumber = phoneNumber;
        Address = fullAddress;
        CustomerType = customerType;
        NameSurname = nameSurname;
        CorporateName = corporateName;
    }

    public string CustomerType { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public int Id { get; init; }
    public string NationalIdentityOrTaxNumber { get; init; } = null!;
    public string NameSurname { get; init; } = null!;
    public string CorporateName { get; init; } = null!;
    public int SalesCount { get; init; }
    public string PhoneNumber { get; set; } = null!;
    public string Address { get; set; } = null!;

}