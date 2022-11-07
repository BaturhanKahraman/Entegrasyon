namespace Entegrasyon.Entity.Dtos.DiscountVouchers;

public record CreateDiscountVoucherDto(decimal Amount, DateTimeOffset? ExpiringDay = null, int? CustomerId = null);

