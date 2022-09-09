using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity
{
    [Index("Identity",IsUnique = true)]
    [Index("Name","Surname",IsUnique = true)]
    public class ApplicationCustomer : ApplicationEntity
    {
        [DataType("char")]
        [StringLength(11)]
        public string Identity { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }

        public List<Sale> Sales { get; set; }

    }
}
