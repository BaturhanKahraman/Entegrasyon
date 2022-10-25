using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Entity;
[Owned]
public sealed class Address
{
    public string City { get; set; }
    public string Country { get; set; }
    public string County { get; set; }
    public string Street { get; set; }
    public string ZipCode { get; set; }
    public string FullAddress { get; set; }
}