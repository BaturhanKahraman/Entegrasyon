namespace Entegrasyon.Entity.Dtos.Customers;

public sealed class CustomerDetailDto
{
    public CustomerDetailDto()
    {
        
    }
    public CustomerDetailDto(DateTimeOffset createdAt, int id, string nationalIdentity, string name, string surname, int salesCount, string phoneNumber, string fullAddress)
    {
        CreatedAt = createdAt;
        Id = id;
        NationalIdentity = nationalIdentity;
        Name = name;
        Surname = surname;
        SalesCount = salesCount;
        PhoneNumber = phoneNumber;
        Address = fullAddress;
    }
    public DateTimeOffset CreatedAt { get; set; }
    public int Id { get; init; }
    public string NationalIdentity { get; init; }
    public string Name { get; init; }
    public string Surname { get; init; }
    public int SalesCount { get; init; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    
}

/*
 *        public string NationalIdentity { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }

        public List<Sale> Sales { get; set; }
 *
 */