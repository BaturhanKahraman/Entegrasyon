namespace Entegrasyon.Entity.Dtos.DiscountVouchers;

public record DiscountVoucherDto(
    int Id,
    string Code,
    double Percentage,
    decimal Amount,
    DateTimeOffset? ExpiringDate,
    string CustomerFullName,
    string PhoneNumber,
    string CustomerType,
    string IdentityNumber,
    string TaxNumber
    );
/*
 *d.Id, d.Code, d.Percentage, d.Amount, d.ExpiringDate.Value, d.Customer.FullName,
                d.Customer.PhoneNumber,
                d.Customer.CustomerType,
                (d.Customer as RetailCustomer).NationalIdentity,
                (d.Customer as CorporateCustomer).TaxNumber
 * 
 */