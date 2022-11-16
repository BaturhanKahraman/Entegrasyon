using Shared.DTO;
using Shared.DTO.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Entity.Dtos
{
    public class DemoDto:IValidatebleDto
    {
        [DisplayDtoPropertyName("Görünen İsim")]
        public string GorunenProperty { get; set; }
        [DtoIgnore]
        public string BununGonderme { get; set; }

    }
}
