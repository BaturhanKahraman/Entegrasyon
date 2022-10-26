using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Orders;

namespace Entegrasyon.Entity.Customers
{
    public class Customer : BaseEntity
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string Discriminator { get; set; }
        public Address Address { get; set; }
        public ICollection<Sale> Sales { get; set; }
    }
}
