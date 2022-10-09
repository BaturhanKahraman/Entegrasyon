using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Logs;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class FluentValidator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationLogManager _applicationLogManager;
    public FluentValidator(IServiceProvider serviceProvider, ApplicationLogManager applicationLogManager)
    {
        _serviceProvider = serviceProvider;
        _applicationLogManager = applicationLogManager;
    }

    public async Task ValidateAndThrowAsync<T>(T entity)
    {
        var validator = (IValidator<T>)_serviceProvider.GetService(typeof(IValidator<T>));
        if(validator==null)
            throw new Exception("Validator bulunamadı");
        var validateResult = await validator.ValidateAsync(entity);
        if (validateResult.IsValid)
            return;
        await _applicationLogManager.AddLog("Doğrulama hatası yakalandı. Hata: " +
            validateResult.Errors.Select(x => x.ErrorMessage)
                .Aggregate((x,y) => x + ", " + y),LogType.Error,0,entity);
        throw new ValidationException(validateResult.Errors);
    }
}