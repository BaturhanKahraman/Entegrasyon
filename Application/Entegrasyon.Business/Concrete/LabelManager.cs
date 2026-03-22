using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Labels;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Labels;
using Entegrasyon.Entity.Results;
using Entegrasyon.PrintAgent.Contracts.Labels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class LabelManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILabelGenerator labelGenerator,
    IReceiptGenerator receiptGenerator,
    ILogger<LabelManager> logger) : ILabelService
{
    public async Task<IDataResult<PrintJobDto>> GenerateProductLabel(Guid variantId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        // 1. Validation
        if (variantId == Guid.Empty)
            return new ErrorDataResult<PrintJobDto>(null, "Geçersiz varyant ID");

        // 2. Business Rules
        var variant = await dbContext.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.ProductVariantAttributes)
            .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted);

        if (variant is null)
            return new ErrorDataResult<PrintJobDto>(null, "Varyant bulunamadı veya silinmiş");

        if (string.IsNullOrWhiteSpace(variant.Barcode))
            return new ErrorDataResult<PrintJobDto>(null, "Varyantın barkodu tanımlı değil");

        // 3. Execution
        var variantInfo = BuildVariantInfo(variant.ProductVariantAttributes);
        var currencySymbol = variant.CurrencyType == "TRY" ? "₺" : variant.CurrencyType;

        // Varsayılan şablonu kontrol et
        var template = await dbContext.LabelTemplates
            .FirstOrDefaultAsync(t => t.Type == LabelType.ProductBarcode && t.IsDefault && !t.IsDeleted);

        string zpl;

        if (template is not null)
        {
            var elements = JsonSerializer.Deserialize<List<LabelElement>>(template.LayoutJson) ?? [];
            var widthDots = (int)(template.WidthMm * template.Dpi / 25.4);
            var heightDots = (int)(template.HeightMm * template.Dpi / 25.4);
            var fieldValues = new Dictionary<string, string>
            {
                ["Title"] = variant.Product?.Title ?? "Ürün",
                ["VariantInfo"] = variantInfo,
                ["Barcode"] = variant.Barcode,
                ["SalePrice"] = $"{variant.SalePrice:N2} {currencySymbol}",
                ["ListPrice"] = $"{variant.ListPrice:N2} {currencySymbol}"
            };
            zpl = labelGenerator.GenerateFromTemplate(elements, widthDots, heightDots, fieldValues);
        }
        else
        {
            // Fallback — hardcoded ZPL
            var labelData = new ProductBarcodeLabelData(
                Barcode: variant.Barcode,
                ProductTitle: variant.Product?.Title ?? "Ürün",
                VariantInfo: variantInfo,
                Price: variant.SalePrice,
                CurrencySymbol: currencySymbol);

            zpl = labelGenerator.GenerateProductBarcode(labelData);
        }

        logger.LogInformation("Barkod etiketi üretildi: Varyant {VariantId}, Barkod {Barcode}",
            variantId, variant.Barcode);

        return new SuccessDataResult<PrintJobDto>(
            new PrintJobDto(zpl, null, "ZPL", $"Barkod: {variant.Barcode}"));
    }

    public async Task<IDataResult<List<PrintJobDto>>> GenerateBulkLabels(List<Guid> variantIds)
    {
        using var dbContext = contextFactory.CreateDbContext();
        // 1. Validation
        if (variantIds is null || variantIds.Count == 0)
            return new ErrorDataResult<List<PrintJobDto>>(null, "En az bir varyant ID gerekli");

        // 2. Business Rules - toplu sorgula
        var variants = await dbContext.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.ProductVariantAttributes)
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync();

        if (variants.Count == 0)
            return new ErrorDataResult<List<PrintJobDto>>(null, "Hiçbir varyant bulunamadı");

        // 3. Execution
        var template = await dbContext.LabelTemplates
            .FirstOrDefaultAsync(t => t.Type == LabelType.ProductBarcode && t.IsDefault && !t.IsDeleted);

        List<LabelElement>? templateElements = null;
        int widthDots = 0, heightDots = 0;

        if (template is not null)
        {
            templateElements = JsonSerializer.Deserialize<List<LabelElement>>(template.LayoutJson) ?? [];
            widthDots = (int)(template.WidthMm * template.Dpi / 25.4);
            heightDots = (int)(template.HeightMm * template.Dpi / 25.4);
        }

        var jobs = new List<PrintJobDto>();

        foreach (var variant in variants)
        {
            if (string.IsNullOrWhiteSpace(variant.Barcode))
            {
                logger.LogWarning("Barkodu olmayan varyant atlandı: {VariantId}", variant.Id);
                continue;
            }

            var variantInfo = BuildVariantInfo(variant.ProductVariantAttributes);
            var currencySymbol = variant.CurrencyType == "TRY" ? "₺" : variant.CurrencyType;

            string zpl;

            if (templateElements is not null)
            {
                var fieldValues = new Dictionary<string, string>
                {
                    ["Title"] = variant.Product?.Title ?? "Ürün",
                    ["VariantInfo"] = variantInfo,
                    ["Barcode"] = variant.Barcode,
                    ["SalePrice"] = $"{variant.SalePrice:N2} {currencySymbol}",
                    ["ListPrice"] = $"{variant.ListPrice:N2} {currencySymbol}"
                };
                zpl = labelGenerator.GenerateFromTemplate(templateElements, widthDots, heightDots, fieldValues);
            }
            else
            {
                var labelData = new ProductBarcodeLabelData(
                    Barcode: variant.Barcode,
                    ProductTitle: variant.Product?.Title ?? "Ürün",
                    VariantInfo: variantInfo,
                    Price: variant.SalePrice,
                    CurrencySymbol: currencySymbol);

                zpl = labelGenerator.GenerateProductBarcode(labelData);
            }

            jobs.Add(new PrintJobDto(zpl, null, "ZPL", $"Barkod: {variant.Barcode}"));
        }

        logger.LogInformation("Toplu etiket üretildi: {Count}/{Total} varyant",
            jobs.Count, variantIds.Count);

        return new SuccessDataResult<List<PrintJobDto>>(jobs,
            $"{jobs.Count} etiket üretildi");
    }

    public async Task<IDataResult<PrintJobDto>> GenerateSaleReceipt(Guid saleId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        // 1. Validation
        if (saleId == Guid.Empty)
            return new ErrorDataResult<PrintJobDto>(null, "Geçersiz satış ID");

        // 2. Business Rules
        var sale = await dbContext.Sales
            .Include(s => s.SaleItems)
                .ThenInclude(si => si.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(s => s.SalePerson)
            .Include(s => s.Customer)
            .Include(s => s.BranchOffice)
            .FirstOrDefaultAsync(s => s.Id == saleId && !s.IsDeleted);

        if (sale is null)
            return new ErrorDataResult<PrintJobDto>(null, "Satış bulunamadı");

        // 3. Execution
        var items = sale.SaleItems.Select(si => new ReceiptLineItem(
            ProductName: si.ProductVariant?.Product?.Title ?? "Ürün",
            Quantity: si.Quantity,
            UnitPrice: si.UnitPrice,
            LineTotal: si.UnitPrice * si.Quantity * (1 - (decimal)si.DiscountPercent / 100)
        )).ToList();

        var subTotal = items.Sum(i => i.UnitPrice * i.Quantity);
        var total = items.Sum(i => i.LineTotal);
        var discount = subTotal - total;

        var receiptData = new SaleReceiptData(
            StoreName: sale.BranchOffice?.Name ?? "Mağaza",
            StoreAddress: "",
            TaxId: "",
            Items: items,
            SubTotal: subTotal,
            Discount: discount,
            Total: total,
            PaymentMethod: "Nakit",
            SaleDate: sale.CreatedAt,
            CashierName: sale.SalePerson?.UserName ?? "Kasiyer",
            CustomerName: sale.Customer?.FullName);

        var receiptBytes = receiptGenerator.GenerateSaleReceipt(receiptData);

        logger.LogInformation("Satış fişi üretildi: Satış {SaleId}, {ItemCount} kalem",
            saleId, items.Count);

        return new SuccessDataResult<PrintJobDto>(
            new PrintJobDto(null, receiptBytes, "ESCPOS", $"Fiş: {saleId:N}"));
    }

    private static string BuildVariantInfo(IEnumerable<Entity.Products.ProductVariantAttribute> attributes)
    {
        if (attributes is null) return string.Empty;
        return string.Join(" / ",
            attributes.Select(a => !string.IsNullOrWhiteSpace(a.CustomValue) ? a.CustomValue : a.CategoryAttributeValue));
    }
}
