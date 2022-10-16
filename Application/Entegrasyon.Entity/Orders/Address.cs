using Shared.Entity;

namespace Entegrasyon.Entity.Orders;
public sealed class Address : BaseEntity
{
    public int Id { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string Street { get; set; }
    public string ZipCode { get; set; }
}