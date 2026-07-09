# Business Pipeline (Validation -> BusinessRules -> Execution)

## 1. Validation (FluentValidation)

```csharp
public class AddProductDtoValidator : AbstractValidator<AddProductDto>
{
    public AddProductDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StockCode).NotEmpty().Matches(@"^[A-Z0-9-]+$");
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleForEach(x => x.ProductVariants).SetValidator(new AddProductVariantDtoValidator());
    }
}
```

`IFluentValidator` projedeki wrapper — `await validator.ValidateAndThrowAsync(dto)` çağrısı validator'ı bulup çalıştırır. Hatalar `ValidationException` atar -> `ValidationExceptionHandler` ProblemDetails'e çevirir (veya HTMX'te alert).

Validator kaydı `ApplicationDependencyExtension` içinde `AddValidatorsFromAssembly(...)` ile otomatik.

## 2. Business Rules (LogicRunner)

`LogicRunner.Run(IResult result1, IResult result2, ...)` — sırayla `Success == false` olan ilk sonucu döner, hepsi başarılıysa `null`.

```csharp
var check = LogicRunner.Run(
    officeStockManager.CheckIfProductCountZero(variants),
    barcodeService.CheckBarcodeUnique(dto.Barcode),
    categoryService.CheckCategoryIsLeaf(dto.CategoryId)
);
if (check != null)
    return new ErrorDataResult<Product>(null!, check.Message!);
```

Her business kontrol metodu `IResult` döner (`SuccessResult` veya `ErrorResult`). DB query lazımsa kontrol fonksiyonu kendi async wrapper'ını sunar; LogicRunner sync olduğu için await'i çağıran tarafta yap, sonucu LogicRunner'a geçir.

## 3. Execution

Sadece üst iki adım geçerse DB / dış sistem etkileşimi:

```csharp
var product = mapper.MapToEntity(dto);                  // Mapperly
dbContext.MainProducts.Add(product);
if (flags.PublishEnabled)
    dbContext.AddDomainEvent(new ProductAddedEvent(product.Id));
await dbContext.SaveChangesAsync(ct);                   // BaseEntity timestamps + UTC otomatik
return new SuccessDataResult<Product>(product);
```

## Result Tipleri

| Tip | Kullanım |
|---|---|
| `IResult` / `Result` | Void operasyon (delete, sync) |
| `IDataResult<T>` / `DataResult<T>` | Veri dönen operasyon |
| `SuccessResult` / `ErrorResult` | Helper constructor'lar |
| `SuccessDataResult<T>(data)` / `ErrorDataResult<T>(null!, message)` | Generic helper'lar |

Controller `result.Success` kontrol eder, `result.Message`'ı `TempData.SetError/SetSuccess`'e basar.

## Çift Loglama (Strict Rule)

```csharp
public async Task<IDataResult<Product>> Add(AddProductDto dto)
{
    // 1. Kullanıcı-facing (admin dashboard'da görünür, Türkçe)
    await applicationLogManager.AddLog(
        "Ürün ekleme isteği geldi.",
        LogType.Product,
        LogAction.Add,
        dto);   // payload JSON serialize edilir

    // 2. Developer-facing (Serilog, OpenTelemetry, exception detayı)
    logger.LogInformation("Adding product {StockCode}", dto.StockCode);

    try
    {
        // ... validation, rules, execution ...
        await applicationLogManager.AddLog(
            $"Ürün eklendi: {product.Title}",
            LogType.Product,
            LogAction.Add);
        logger.LogInformation("Product {Id} added successfully", product.Id);
        return new SuccessDataResult<Product>(product);
    }
    catch (Exception ex)
    {
        // Kullanıcı için temiz mesaj (stack trace YOK)
        await applicationLogManager.AddLog(
            "Ürün eklenirken hata oluştu.",
            LogType.Product,
            LogAction.Add);
        // Dev için tam detay
        logger.LogError(ex, "Failed to add product {StockCode}", dto.StockCode);
        throw;
    }
}
```

**Not:** `IApplicationLogManager.AddLog` şu an senkron DB yazımı yapıyor (iş transaction'ı içinde). İleride `Channel<T>` pattern ile async queue'ya geçirilecek — yeni kod yazarken bu varsayımla yazma, ama bil ki gelecek.

## Mapperly Kullanımı

```csharp
[Mapper]
public partial class ProductMapper
{
    public partial Product MapToEntity(AddProductDto dto);
    public partial ProductsDetailDto MapToDetailDto(Product entity);
    public partial List<ProductListDto> MapToListDto(IEnumerable<Product> entities);
}
```

`Business/Mappers/` altında. Source-generator compile-time çalışır; runtime cost yok. Mapster KALDIRILDI — yeni mapping Mapperly ile yazılır.

## BaseEntity ve UTC

Tüm entity'ler `BaseEntity`'den türer:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

`DbContext.SaveChangesAsync()` override:
- Tüm `DateTime` field'larını UTC'ye çevirir
- `BaseEntity.CreatedAt` / `UpdatedAt` otomatik set eder
- Domain event'leri Channel'a yazıp commit eder

Yeni entity yazarken `BaseEntity`'den türetmeyi unutma. Migration `dotnet ef migrations add <Name> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`.

## No-Tracking Default

`DbContext` `QueryTrackingBehavior.NoTracking` ile kayıtlı. Update için tracking lazımsa:
```csharp
var product = await dbContext.MainProducts
    .AsTracking()
    .FirstOrDefaultAsync(p => p.Id == id);
```

Veya `Update(entity)` + `SaveChanges` — attached state'e geçirir.
