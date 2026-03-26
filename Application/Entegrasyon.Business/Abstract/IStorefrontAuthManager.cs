using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontAuthManager
{
    Task<IDataResult<StorefrontCustomerAuth>> RegisterAsync(StorefrontRegisterDto dto);
    Task<IDataResult<StorefrontCustomerAuth>> LoginAsync(int tenantId, string email, string password);
    Task<IResult> ConfirmEmailAsync(int tenantId, string token);
    Task<IDataResult<string>> RequestPasswordResetAsync(int tenantId, string email);
    Task<IResult> ResetPasswordAsync(int tenantId, string token, string newPassword);
    Task<IResult> ChangePasswordAsync(int authId, string currentPassword, string newPassword);
    Task<IDataResult<StorefrontCustomerAuth>> GetAuthByCustomerIdAsync(int tenantId, int customerId);
    Task<IResult> UpdateProfileAsync(int customerId, StorefrontProfileDto dto);
}
