using Entegrasyon.Entity;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontGiftCardManager
{
    Task<IDataResult<StorefrontGiftCard>> CreateGiftCardAsync(int tenantId, decimal amount, int? purchasedByCustomerId, string? recipientEmail, string? recipientName, string? message);
    Task<IDataResult<StorefrontGiftCard>> GetByCodeAsync(int tenantId, string code);
    Task<IDataResult<decimal>> UseGiftCardAsync(int tenantId, string code, decimal amount, Guid orderId);
    Task<IDataResult<decimal>> CheckBalanceAsync(int tenantId, string code);
    Task<IDataResult<Pageable<StorefrontGiftCard>>> GetGiftCardsAsync(int tenantId, int pageIndex = 0, int pageSize = 20);
    Task<IDataResult<StorefrontGiftCard>> GetByIdAsync(int tenantId, int id);
    Task<IDataResult<List<StorefrontGiftCardTransaction>>> GetTransactionsAsync(int giftCardId);
}
