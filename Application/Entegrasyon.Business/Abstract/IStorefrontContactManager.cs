using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontContactManager
{
    Task<IResult> SubmitMessageAsync(int tenantId, string name, string email, string? phone, string? subject, string message);
    Task<IDataResult<List<StorefrontContactMessage>>> GetMessagesAsync(int tenantId);
    Task<IResult> MarkAsReadAsync(int id);
}
