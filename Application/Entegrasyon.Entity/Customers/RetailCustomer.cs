using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Entity.Customers
{
    public class RetailCustomer:Customer
    {
        public string NationalIdentity { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }

        public string FullName => Name + " " + Surname;
    }
}
