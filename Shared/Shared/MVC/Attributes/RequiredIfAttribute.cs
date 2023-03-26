using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Shared.MVC.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RequiredIfAttribute : ValidationAttribute
{
    private readonly string _otherProperty;
    private readonly object _targetValue;
    private readonly ComparisonType _comparisonType;

    public RequiredIfAttribute(string otherProperty,ComparisonType comparisonType,object targetValue)
    {
        _otherProperty = otherProperty;
        _targetValue = targetValue;
        _comparisonType = comparisonType;
    }

    protected override ValidationResult IsValid(object value,ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(_otherProperty);

        if(property == null)
        {
            return new ValidationResult($"Property {_otherProperty} not found.");
        }

        var targetValue = property.GetValue(validationContext.ObjectInstance);
        var comparisonResult = CompareValues(targetValue,_targetValue,_comparisonType);

        if(comparisonResult)
        {
            if(value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(this.ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }

    private bool CompareValues(object value1,object value2,ComparisonType comparisonType)
    {
        switch(comparisonType)
        {
            case ComparisonType.EqualTo:
                return Equals(value1,value2);
            case ComparisonType.NotEqualTo:
                return !Equals(value1,value2);
            case ComparisonType.LessThan:
                return Comparer.Default.Compare(value1,value2) < 0;
            case ComparisonType.LessThanOrEqualTo:
                return Comparer.Default.Compare(value1,value2) <= 0;
            case ComparisonType.GreaterThan:
                return Comparer.Default.Compare(value1,value2) > 0;
            case ComparisonType.GreaterThanOrEqualTo:
                return Comparer.Default.Compare(value1,value2) >= 0;
            default:
                throw new ArgumentException("Invalid comparison type.",nameof(comparisonType));
        }
    }
}