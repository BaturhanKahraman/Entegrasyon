using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Utility.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ExclusiveBooleanAttribute : ValidationAttribute
{
    private readonly string _comparedBooleanProp;
    public ExclusiveBooleanAttribute(string nameOfComparedBoolProp)
    {
        if(string.IsNullOrEmpty(nameOfComparedBoolProp))
            ArgumentException.ThrowIfNullOrEmpty(nameof(nameOfComparedBoolProp));
        _comparedBooleanProp = nameOfComparedBoolProp;
    }
    protected override ValidationResult IsValid(object value,ValidationContext validationContext)
    {
        if(value is not bool boolValue)
            throw new InvalidOperationException("Bu attribute yalnızca boolean değişkenler için kullanılabilir.");
        var otherPropertyInfo = validationContext.ObjectType.GetProperty(_comparedBooleanProp);
        if(otherPropertyInfo == null)
            throw new InvalidOperationException(_comparedBooleanProp + " bulunamadı");
        bool? otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance) as bool?;
        if(!otherPropertyValue.HasValue ||
            (otherPropertyValue == false && boolValue == false) || 
            otherPropertyValue != boolValue)
            return ValidationResult.Success;

        return new ValidationResult($"{_comparedBooleanProp} değeri ve {validationContext.MemberName} özelliklerinin ikisi de aynı anda açık olamaz.");
    }
}
