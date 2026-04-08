using Entegrasyon.Entity.DiscountVouchers;

namespace Entegrasyon.Entity.Dtos.DiscountVouchers;

public record CreateDiscountVoucherDto(
    DiscountType DiscountType,
    decimal Amount,
    double Percentage,
    DateTimeOffset? ExpiringDay = null,
    int? CustomerId = null,
    int? MaxUsageCount = null,
    decimal? MinimumCartAmount = null);
