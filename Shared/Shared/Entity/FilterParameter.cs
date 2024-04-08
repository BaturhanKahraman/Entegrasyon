using Shared.Results;

namespace Shared.Entity;

public sealed record FilterParameter
{
    public required string Field { get; init; }
    public required FilterOperator Operator { get; init; }
    public required string  Value { get; init; }

    public IResult Validate()
    {
        // Operatöre bağlı doğrulamalar
        switch (Operator)
        {
            case FilterOperator.GreaterThan:
            case FilterOperator.LessThan:
                if (!IsNumeric(Value))
                {
                    return new ErrorResult($"{Field} alanı numerik bir değer olmalı.");
                }
                break;
            default:
                break;
        }
        static bool IsNumeric(string value) => double.TryParse(value, out _);

        return new SuccessResult();
    }
}

