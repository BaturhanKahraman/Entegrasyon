using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Entity;

namespace Entegrasyon.Entity.Sales
{
    public class Sale:GuidEntity
    {
        public List<SaleItem> SaleItems { get; set; }
        public Guid SalePersonId { get; set; }
        public ApplicationUser SalePerson { get; set; }
    }
}
