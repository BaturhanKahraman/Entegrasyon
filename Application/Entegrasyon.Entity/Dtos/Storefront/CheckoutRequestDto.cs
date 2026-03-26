namespace Entegrasyon.Entity.Dtos.Storefront;

public record CheckoutRequestDto(
    string ShippingFullName, string ShippingPhone,
    string ShippingCity, string ShippingDistrict, string ShippingAddress,
    string? ShippingPostalCode, bool UseSameAddressForBilling,
    string? BillingFullName, string? BillingCity, string? BillingAddress,
    string? OrderNote,
    bool IsGiftWrapped = false, string? GiftMessage = null, bool HideInvoice = false);
