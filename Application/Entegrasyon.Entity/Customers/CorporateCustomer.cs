using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Entity.Customers
{
    public class CorporateCustomer:Customer
    {
        public string TaxNumber { get; set; }
        public string CorporateName { get; set; }

    }
}
