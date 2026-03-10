using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.Utility.Attributes.Validations;

[AttributeUsage(AttributeTargets.Property)]
public class ExclusiveBooleanAttribute : ValidationAttribute
{
    private readonly string comparedBooleanProp;
    public ExclusiveBooleanAttribute(string nameOfComparedBoolProp)
    {
        if (string.IsNullOrEmpty(nameOfComparedBoolProp))
            ArgumentException.ThrowIfNullOrEmpty(nameof(nameOfComparedBoolProp));
        comparedBooleanProp = nameOfComparedBoolProp;
    }
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not bool boolValue)
            throw new InvalidOperationException("Bu attribute yalnızca boolean değişkenler için kullanılabilir.");
        var otherPropertyInfo = validationContext.ObjectType.GetProperty(comparedBooleanProp);
        if (otherPropertyInfo == null)
            throw new InvalidOperationException(comparedBooleanProp + " bulunamadı");
        bool? otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance) as bool?;
        if (!otherPropertyValue.HasValue ||
            otherPropertyValue == false && boolValue == false ||
            otherPropertyValue != boolValue)
            return ValidationResult.Success!;
        //take display names
        string comparedObjName = otherPropertyInfo.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() is DisplayAttribute displayAttr ? displayAttr.GetName() ?? otherPropertyInfo.Name : otherPropertyInfo.Name;
        return new ValidationResult($"{comparedObjName} değeri ve {validationContext.DisplayName} özelliklerinin ikisi de aynı anda açık olamaz.");
    }
}
