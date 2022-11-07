using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.DiscountVouchers;

namespace Entegrasyon.Entity.Customers
{
    public class Customer : BaseEntity
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string CustomerType { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string FullName { get; set; }
        public Address Address { get; set; }
        public ICollection<Sale> Sales { get; set; }
        public ICollection<DiscountVoucher> DiscountVouchers { get; set; }
    }
}
