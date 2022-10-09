using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity
{
    public class ApplicationCustomer : ApplicationEntity
    {
        [DataType("char")]
        [StringLength(11)]
        public string NationalIdentity { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }

        public NpgsqlTsVector SearchVector { get; set; }

        public List<Sale> Sales { get; set; }

    }
}
