using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Entity;
[Owned]
public sealed class Address
{
    public string City { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string County { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string ZipCode { get; set; } = null!;
    public string FullAddress { get; set; } = null!;
}