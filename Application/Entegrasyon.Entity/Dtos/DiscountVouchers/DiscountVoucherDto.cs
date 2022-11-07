namespace Entegrasyon.Entity.Dtos.DiscountVouchers;

public record DiscountVoucherDto(
    int Id,
    string Code,
    double Percentage,
    decimal Amount,
    DateTimeOffset? ExpiringDate,
    string customerFullName,
    string phoneNumber,
    string IdentityNumber,
    string TaxNumber
    );
