// Business/Concrete/ProductManager.cs
// Strict Rule Pipeline: Validation -> Business Rules -> Execution
// Çift loglama: IApplicationLogManager (user-facing) + ILogger<T> (dev-facing)
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Utilities;            // LogicRunner
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class ProductManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,        // KULLANICI-FACING log (DB, TR, admin görür)
    ILogger<ProductManager> logger,                       // DEV-FACING log (Serilog, OTel, stack trace)
    IFluentValidator validator,
    IOfficeStockManager officeStockManager,
    IBarcodeService barcodeService,
    ProductMapper mapper) : IProductService
{
    public async Task<IDataResult<Product>> Add(AddProductDto dto)
    {
        // ── 0. ÇİFT LOG (başlangıç) ──────────────────────────────────────
        await applicationLogManager.AddLog(
            "Ürün ekleme isteği geldi.",
            LogType.Product,
            LogAction.Add,
            dto);                                          // payload JSON serialize
        logger.LogInformation("Adding product {StockCode}", dto.StockCode);

        try
        {
            // ── 1. VALIDATION (FluentValidation) ─────────────────────────
            await validator.ValidateAndThrowAsync(dto);
            // ValidationException -> ValidationExceptionHandler ProblemDetails

            await using var dbContext = await contextFactory.CreateDbContextAsync();

            // ── 2. BUSINESS RULES (LogicRunner + ad-hoc) ─────────────────
            var stockCodeConflict = !string.IsNullOrEmpty(dto.StockCode) &&
                await dbContext.MainProducts
                    .AnyAsync(p => p.StockCode == dto.StockCode && !p.IsDeleted);
            if (stockCodeConflict)
                return new ErrorDataResult<Product>(null!,
                    "Bu stok kodu zaten kullanılıyor. Lütfen farklı bir stok kodu girin.");

            var hasChildren = await dbContext.Categories
                .AnyAsync(c => c.SuperCategoryId == dto.CategoryId && !c.IsDeleted);
            if (hasChildren)
                return new ErrorDataResult<Product>(null!,
                    "Sadece alt kategorisi olmayan (yaprak) kategoriler seçilebilir.");

            var check = LogicRunner.Run(
                officeStockManager.CheckIfProductCountZero(
                    dto.ProductVariants.SelectMany(v => v.BranchOfficeStocks).ToArray())
            );
            if (check != null)
                return new ErrorDataResult<Product>(null!, check.Message!);

            // ── 3. EXECUTION ─────────────────────────────────────────────
            foreach (var v in dto.ProductVariants.Where(v => string.IsNullOrEmpty(v.Barcode)))
                v.Barcode = await barcodeService.GenerateAsync();

            var product = mapper.MapToEntity(dto);          // Mapperly (source-gen)

            dbContext.MainProducts.Add(product);
            await dbContext.SaveChangesAsync();             // BaseEntity UTC + timestamps otomatik

            // ── 4. ÇİFT LOG (başarı) ─────────────────────────────────────
            await applicationLogManager.AddLog(
                $"Ürün eklendi: {product.Title}",
                LogType.Product,
                LogAction.Add);
            logger.LogInformation("Product {Id} added", product.Id);

            return new SuccessDataResult<Product>(product);
        }
        catch (Exception ex)
        {
            // Kullanıcı için temiz TR mesaj (stack trace YOK)
            await applicationLogManager.AddLog(
                "Ürün eklenirken hata oluştu.",
                LogType.Product,
                LogAction.Add);
            // Dev için tam detay
            logger.LogError(ex, "Failed to add product {StockCode}", dto.StockCode);
            throw;                                          // Exception handler chain'e bırak
        }
    }
}

// ── Validator (Business/Validation/FluentValidation/AddProductDtoValidator.cs) ──
//
// public class AddProductDtoValidator : AbstractValidator<AddProductDto>
// {
//     public AddProductDtoValidator()
//     {
//         RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
//         RuleFor(x => x.StockCode).NotEmpty().Matches(@"^[A-Z0-9-]+$");
//         RuleFor(x => x.CategoryId).GreaterThan(0);
//         RuleForEach(x => x.ProductVariants)
//             .SetValidator(new AddProductVariantDtoValidator());
//     }
// }
//
// ApplicationDependencyExtension.AddApplicationDependencies() içinde
// services.AddValidatorsFromAssembly(typeof(IProductService).Assembly);
// IFluentValidator (wrapper) DI'a otomatik kayıtlı.
