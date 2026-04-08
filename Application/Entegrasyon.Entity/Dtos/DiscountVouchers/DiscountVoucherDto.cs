using Entegrasyon.Entity.DiscountVouchers;

namespace Entegrasyon.Entity.Dtos.DiscountVouchers;

public record DiscountVoucherDto(
    int Id,
    string Code,
    DiscountType DiscountType,
    double Percentage,
    decimal Amount,
    DateTimeOffset? ExpiringDate,
    bool IsActive,
    int? MaxUsageCount,
    int CurrentUsageCount,
    decimal? MinimumCartAmount,
    string? CustomerFullName);
