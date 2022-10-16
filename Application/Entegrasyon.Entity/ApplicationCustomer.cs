using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Orders;

namespace Entegrasyon.Entity
{
    public sealed class ApplicationCustomer : BaseEntity
    {
        public int Id { get; set; }
        [DataType("char")]
        [StringLength(11)]
        public string NationalIdentity { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public NpgsqlTsVector SearchVector { get; set; }

        public List<Sale> Sales { get; set; }

    }
}
