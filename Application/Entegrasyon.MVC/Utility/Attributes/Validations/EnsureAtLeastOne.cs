using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Utility.Attributes.Validations
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple = false)]
    public class EnsureAtLeastOne:ValidationAttribute
    {
        public EnsureAtLeastOne()
        {
        }
        public EnsureAtLeastOne(string errorMessage):base(errorMessage)
        {
            
        }
        public override bool IsValid(object value)
        {
            if(value is ICollection values)
                return values.Count > 0;
            throw new Exception("Property must be ICollection");
        }
    }
}
