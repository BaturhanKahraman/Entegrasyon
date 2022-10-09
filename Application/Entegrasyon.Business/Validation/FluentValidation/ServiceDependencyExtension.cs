using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Products;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Business.Validation.FluentValidation;

public static class ServiceDependencyExtension
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<BranchOffice>,BranchValidator>();
        services.AddScoped<IValidator<AddRoleDto>,AddRoleDtoValidator>();
        services.AddScoped<IValidator<AddUserDto>,AddUserDtoValidator>();
        services.AddScoped<IValidator<Brand>,BrandValidator>();
        services.AddScoped<IValidator<AddCustomerDto>,AddCustomerDtoValidator>();
        services.AddScoped<IValidator<UpdateCustomerDto>,UpdateCustomerDtoValidator>();
        services.AddTransient<FluentValidator>();
        return services;
    }
}