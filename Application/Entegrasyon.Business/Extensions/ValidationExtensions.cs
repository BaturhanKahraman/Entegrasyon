using FluentValidation.Results;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Extensions;

public static class ValidationExtensions
{
    public static IResult ToResult(this ValidationResult validationResult)
    {
        return new Result(validationResult.IsValid, string.Join(',',validationResult.Errors.Select(e => e.ErrorMessage)));
    }
}