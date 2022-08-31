using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Orders;
public class Address : ApplicationEntity
{
    public string City { get; set; }
    public string Country { get; set; }
    public string Street { get; set; }
    public string ZipCode { get; set; }
}