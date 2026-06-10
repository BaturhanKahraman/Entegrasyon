namespace Entegrasyon.Entity.Dtos.Customers;

// Tip'e göre alanların bir kısmı boş kalır (bireyselde firma/vergi yok, kurumsalda
// ad/soyad/TC opsiyonel). Non-nullable string'ler model binder tarafından "implicitly
// required" sayılıp boş gönderildiğinde ModelState'i geçersiz kılıyordu → AutoValidationFilter
// create'i action'a ulaşmadan geri yönlendiriyordu. Koşullu alanlar nullable yapıldı;
// zorunluluk FluentValidation'da tip'e göre kontrol edilir. CustomerType her zaman gönderilir.
public sealed record CustomerAddDto(
    string? NationalIdentity,
    string? TaxNumber,
    string? Name,
    string? Surname,
    string? CorporateName,
    string? PhoneNumber,
    string? FullAddress,
    string CustomerType);
