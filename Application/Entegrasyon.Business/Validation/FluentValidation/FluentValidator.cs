using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Logs;
using FluentValidation;
using FluentValidation.Results;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class FluentValidator(IServiceProvider serviceProvider, IApplicationLogManager applicationLogManager)
{
    public async Task ValidateAndThrowAsync<T>(T entity)
    {
        if(entity == null)
            throw new ValidationException(new ValidationFailure[1]{new("Object","Obje boş geldi. Lütfen geliştirici ile irtibata geçin.")});
        var validator = (IValidator<T>)serviceProvider.GetService(typeof(IValidator<T>));
        if(validator==null)
            throw new Exception("Validator not found!");
        var validateResult = await validator.ValidateAsync(entity);
        if (validateResult.IsValid)
            return;
        await applicationLogManager.AddLog("Doğrulama hatası yakalandı. Hata: " +
            validateResult.Errors.Select(x => x.ErrorMessage)
                .Aggregate((x,y) => x + ", " + y),LogType.Error,0,entity);
        throw new ValidationException(validateResult.Errors);
    }

    public async ValueTask<ValidationResult> Validate<T>(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var validator = (IValidator<T>)serviceProvider.GetService(typeof(IValidator<T>));
        return validator == null ? throw new Exception("Validator not found!") : await validator.ValidateAsync(entity);
    }
}