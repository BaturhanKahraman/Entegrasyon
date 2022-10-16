using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Entity;

namespace Entegrasyon.Entity.Sales
{
    public sealed class Sale : BaseEntity
    {
        public Guid Id { get; set; }
        public ICollection<SaleItem> SaleItems { get; set; }
        public Guid SalePersonId { get; set; }
        public ApplicationUser SalePerson { get; set; }

        public int? CustomerId { get; set; }
        public ApplicationCustomer Customer { get; set; }
    }
}
