using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;

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


    }
}
