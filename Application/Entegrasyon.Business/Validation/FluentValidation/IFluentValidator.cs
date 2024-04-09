using FluentValidation.Results;

namespace Entegrasyon.Business.Validation.FluentValidation
{
    public interface IFluentValidator
    {
        ValueTask<ValidationResult> Validate<T>(T entity);
        Task ValidateAndThrowAsync<T>(T entity);
    }
}