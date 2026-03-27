using System.Text.Json;
using ClosedXML.Excel;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public sealed class BulkOperationManager(
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ExcelParser excelParser,
    ProductImportValidator importValidator,
    IApplicationLogManager applicationLogManager,
    ILogger<BulkOperationManager> logger) : IBulkOperationManager
{
    public async Task<IDataResult<BulkImportResultDto>> ImportProductsAsync(Stream excelStream, string fileName, Guid userId)
    {
        // 1. Parse
        var parseResult = excelParser.ParseProductImport(excelStream);
        if (!parseResult.Success)
            return new ErrorDataResult<BulkImportResultDto>(null!, parseResult.Message!);

        var rows = parseResult.Data;
        if (rows.Count == 0)
            return new ErrorDataResult<BulkImportResultDto>(null!, "Excel dosyası boş veya veri satırı bulunamadı.");

        // 2. Validate
        var validationErrors = importValidator.ValidateProductRows(rows);
        var invalidBarcodes = validationErrors.Select(e => e.Barcode).ToHashSet();
        var validRows = rows.Where(r => !invalidBarcodes.Contains(r.Barcode)).ToList();

        // 3. Check duplicates within file
        var duplicateErrors = new List<BulkImportRowErrorDto>();
        var seenBarcodes = new HashSet<string>();
        var deduplicatedRows = new List<ProductImportRow>();

        foreach (var row in validRows)
        {
            if (!seenBarcodes.Add(row.Barcode))
            {
                duplicateErrors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Dosya içinde tekrarlayan barkod."));
                continue;
            }
            deduplicatedRows.Add(row);
        }

        var allErrors = validationErrors.Concat(duplicateErrors).OrderBy(e => e.RowNumber).ToList();
        validRows = deduplicatedRows;

        // 4. Execute
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var log = new BulkOperationLog
        {
            OperationType = BulkOperationType.ProductImport,
            FileName = fileName,
            TotalRows = rows.Count,
            Status = BulkOperationStatus.Processing,
            StartedAt = DateTimeOffset.UtcNow,
            StartedByUserId = userId
        };

        dbContext.BulkOperationLogs.Add(log);
        await dbContext.SaveChangesAsync();

        try
        {
            if (validRows.Count > 0)
            {
                var conn = (NpgsqlConnection)dbContext.Database.GetDbConnection();
                await conn.OpenAsync();

                var tempTable = $"tmp_product_import_{Guid.NewGuid():N}";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"""
                    CREATE TEMP TABLE "{tempTable}" (
                        barcode text NOT NULL,
                        title text NOT NULL,
                        stock_code text,
                        list_price numeric NOT NULL,
                        sale_price numeric NOT NULL,
                        cost_price numeric NOT NULL,
                        vat_rate numeric NOT NULL
                    )
                    """;
                await cmd.ExecuteNonQueryAsync();

                await using var writer = await conn.BeginBinaryImportAsync(
                    $"COPY \"{tempTable}\" (barcode, title, stock_code, list_price, sale_price, cost_price, vat_rate) FROM STDIN (FORMAT BINARY)");

                foreach (var row in validRows)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(row.Barcode, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(row.Title, NpgsqlTypes.NpgsqlDbType.Text);
                    if (row.StockCode is not null)
                        await writer.WriteAsync(row.StockCode, NpgsqlTypes.NpgsqlDbType.Text);
                    else
                        await writer.WriteNullAsync();
                    await writer.WriteAsync(row.ListPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                    await writer.WriteAsync(row.SalePrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                    await writer.WriteAsync(row.CostPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
                    await writer.WriteAsync(row.VatRate, NpgsqlTypes.NpgsqlDbType.Numeric);
                }

                await writer.CompleteAsync();

                // Upsert: update existing variants by barcode, skip inserts (products need parent Product)
                await using var upsertCmd = conn.CreateCommand();
                upsertCmd.CommandText = $"""
                    UPDATE "ProductVariants" pv
                    SET "ListPrice" = t.list_price,
                        "SalePrice" = t.sale_price,
                        "CostPrice" = t.cost_price,
                        "VatRate" = t.vat_rate,
                        "UpdatedAt" = NOW()
                    FROM "{tempTable}" t
                    WHERE pv."Barcode" = t.barcode
                      AND NOT pv."IsDeleted";

                    DROP TABLE IF EXISTS "{tempTable}";
                    """;
                await upsertCmd.ExecuteNonQueryAsync();
            }

            log.SuccessCount = validRows.Count;
            log.ErrorCount = allErrors.Count;
            log.Status = allErrors.Count > 0 ? BulkOperationStatus.CompletedWithErrors : BulkOperationStatus.Completed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorDetails = allErrors.Count > 0 ? JsonSerializer.Serialize(allErrors) : null;

            dbContext.BulkOperationLogs.Update(log);
            await dbContext.SaveChangesAsync();

            await applicationLogManager.AddLog(
                $"Toplu ürün içe aktarma tamamlandı: {validRows.Count} başarılı, {allErrors.Count} hata ({fileName})",
                LogType.Product, LogAction.Update);

            logger.LogInformation("Bulk product import completed: {SuccessCount} success, {ErrorCount} errors for file {FileName}",
                validRows.Count, allErrors.Count, fileName);

            return new SuccessDataResult<BulkImportResultDto>(
                new BulkImportResultDto(log.Id, rows.Count, validRows.Count, allErrors.Count, allErrors));
        }
        catch (Exception ex)
        {
            log.Status = BulkOperationStatus.Failed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorDetails = JsonSerializer.Serialize(new { error = ex.Message });

            dbContext.BulkOperationLogs.Update(log);
            await dbContext.SaveChangesAsync();

            logger.LogError(ex, "Bulk product import failed for file {FileName}", fileName);
            await applicationLogManager.AddLog(
                $"Toplu ürün içe aktarma başarısız: {fileName} - {ex.Message}",
                LogType.Error, LogAction.Update);

            return new ErrorDataResult<BulkImportResultDto>(null!, $"İçe aktarma sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<IDataResult<BulkImportResultDto>> ImportPricesAsync(Stream excelStream, string fileName, Guid userId)
    {
        var parseResult = excelParser.ParsePriceImport(excelStream);
        if (!parseResult.Success)
            return new ErrorDataResult<BulkImportResultDto>(null!, parseResult.Message!);

        var rows = parseResult.Data;
        if (rows.Count == 0)
            return new ErrorDataResult<BulkImportResultDto>(null!, "Excel dosyası boş veya veri satırı bulunamadı.");

        var validationErrors = importValidator.ValidatePriceRows(rows);
        var invalidBarcodes = validationErrors.Select(e => e.Barcode).ToHashSet();
        var validRows = rows.Where(r => !invalidBarcodes.Contains(r.Barcode)).ToList();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var log = new BulkOperationLog
        {
            OperationType = BulkOperationType.PriceImport,
            FileName = fileName,
            TotalRows = rows.Count,
            Status = BulkOperationStatus.Processing,
            StartedAt = DateTimeOffset.UtcNow,
            StartedByUserId = userId
        };

        dbContext.BulkOperationLogs.Add(log);
        await dbContext.SaveChangesAsync();

        try
        {
            var barcodes = validRows.Select(r => r.Barcode).ToList();
            var existingVariants = await dbContext.ProductVariants
                .Where(pv => barcodes.Contains(pv.Barcode!))
                .ToListAsync();

            var existingBarcodeSet = existingVariants.Select(v => v.Barcode).ToHashSet();
            var notFoundErrors = validRows
                .Where(r => !existingBarcodeSet.Contains(r.Barcode))
                .Select(r => new BulkImportRowErrorDto(r.RowNumber, r.Barcode, "Barkod sistemde bulunamadı."))
                .ToList();

            var allErrors = validationErrors.Concat(notFoundErrors).OrderBy(e => e.RowNumber).ToList();

            var barcodeToRow = validRows.ToDictionary(r => r.Barcode);
            foreach (var variant in existingVariants)
            {
                if (barcodeToRow.TryGetValue(variant.Barcode!, out var row))
                {
                    variant.ListPrice = row.ListPrice;
                    variant.SalePrice = row.SalePrice;
                    variant.CostPrice = row.CostPrice;
                }
            }

            dbContext.ProductVariants.UpdateRange(existingVariants);
            await dbContext.SaveChangesAsync();

            var successCount = existingVariants.Count;
            log.SuccessCount = successCount;
            log.ErrorCount = allErrors.Count;
            log.Status = allErrors.Count > 0 ? BulkOperationStatus.CompletedWithErrors : BulkOperationStatus.Completed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorDetails = allErrors.Count > 0 ? JsonSerializer.Serialize(allErrors) : null;

            dbContext.BulkOperationLogs.Update(log);
            await dbContext.SaveChangesAsync();

            await applicationLogManager.AddLog(
                $"Toplu fiyat güncelleme tamamlandı: {successCount} başarılı, {allErrors.Count} hata ({fileName})",
                LogType.Product, LogAction.Update);

            logger.LogInformation("Bulk price import completed: {SuccessCount} success, {ErrorCount} errors for file {FileName}",
                successCount, allErrors.Count, fileName);

            return new SuccessDataResult<BulkImportResultDto>(
                new BulkImportResultDto(log.Id, rows.Count, successCount, allErrors.Count, allErrors));
        }
        catch (Exception ex)
        {
            log.Status = BulkOperationStatus.Failed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorDetails = JsonSerializer.Serialize(new { error = ex.Message });
            dbContext.BulkOperationLogs.Update(log);
            await dbContext.SaveChangesAsync();

            logger.LogError(ex, "Bulk price import failed for file {FileName}", fileName);
            return new ErrorDataResult<BulkImportResultDto>(null!, $"Fiyat güncelleme sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<IDataResult<BulkImportResultDto>> ImportStockAsync(Stream excelStream, string fileName, Guid userId)
    {
        var parseResult = excelParser.ParseStockImport(excelStream);
        if (!parseResult.Success)
            return new ErrorDataResult<BulkImportResultDto>(null!, parseResult.Message!);

        var rows = parseResult.Data;
        if (rows.Count == 0)
            return new ErrorDataResult<BulkImportResultDto>(null!, "Excel dosyası boş veya veri satırı bulunamadı.");

        var validationErrors = importValidator.ValidateStockRows(rows);
        var invalidBarcodes = validationErrors.Select(e => e.Barcode).ToHashSet();
        var validRows = rows.Where(r => !invalidBarcodes.Contains(r.Barcode)).ToList();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var log = new BulkOperationLog
        {
            OperationType = BulkOperationType.StockImport,
            FileName = fileName,
            TotalRows = rows.Count,
            Status = BulkOperationStatus.Processing,
            StartedAt = DateTimeOffset.UtcNow,
            StartedByUserId = userId
        };

        dbContext.BulkOperationLogs.Add(log);
        await dbContext.SaveChangesAsync();

        try
        {
            var barcodes = validRows.Select(r => r.Barcode).ToList();
            var variants = await dbContext.ProductVariants
                .Where(pv => barcodes.Contains(pv.Barcode!))
                .Select(pv => new { pv.Id, pv.Barcode })
                .ToListAsync();

            var barcodeToVariantId = variants.ToDictionary(v => v.Barcode!, v => v.Id);
            var notFoundErrors = validRows
                .Where(r => !barcodeToVariantId.ContainsKey(r.Barcode))
                .Select(r => new BulkImportRowErrorDto(r.RowNumber, r.Barcode, "Barkod sistemde bulunamadı."))
                .ToList();

            // Batch-load all relevant stocks in one query instead of N+1
            var matchedVariantIds = validRows
                .Where(r => barcodeToVariantId.ContainsKey(r.Barcode))
                .Select(r => barcodeToVariantId[r.Barcode])
                .Distinct()
                .ToList();

            var allStocks = await dbContext.BranchOfficeStocks
                .Where(s => s.ProductVariantId.HasValue && matchedVariantIds.Contains(s.ProductVariantId.Value))
                .ToListAsync();

            var stockLookup = allStocks
                .Where(s => s.ProductVariantId.HasValue)
                .ToDictionary(s => (s.ProductVariantId!.Value, s.BranchOfficeId));

            var successCount = 0;
            foreach (var row in validRows.Where(r => barcodeToVariantId.ContainsKey(r.Barcode)))
            {
                var variantId = barcodeToVariantId[row.Barcode];
                if (stockLookup.TryGetValue((variantId, row.BranchOfficeId), out var stock))
                {
                    stock.FirstTotalStock = row.Quantity;
                    successCount++;
                }
                else
                {
                    notFoundErrors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode,
                        $"Şube {row.BranchOfficeId} için stok kaydı bulunamadı."));
                }
            }

            dbContext.BranchOfficeStocks.UpdateRange(allStocks.Where(s => dbContext.Entry(s).State == EntityState.Modified));

            await dbContext.SaveChangesAsync();

            var allErrors = validationErrors.Concat(notFoundErrors).OrderBy(e => e.RowNumber).ToList();

            log.SuccessCount = successCount;
            log.ErrorCount = allErrors.Count;
            log.Status = allErrors.Count > 0 ? BulkOperationStatus.CompletedWithErrors : BulkOperationStatus.Completed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorDetails = allErrors.Count > 0 ? JsonSerializer.Serialize(allErrors) : null;

            dbContext.BulkOperationLogs.Update(log);
            await dbContext.SaveChangesAsync();

            await applicationLogManager.AddLog(
                $"Toplu stok güncelleme tamamlandı: {successCount} başarılı, {allErrors.Count} hata ({fileName})",
                LogType.Product, LogAction.Update);

            logger.LogInformation("Bulk stock import completed: {SuccessCount} success, {ErrorCount} errors for file {FileName}",
                successCount, allErrors.Count, fileName);

            return new SuccessDataResult<BulkImportResultDto>(
                new BulkImportResultDto(log.Id, rows.Count, successCount, allErrors.Count, allErrors));
        }
        catch (Exception ex)
        {
            log.Status = BulkOperationStatus.Failed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorDetails = JsonSerializer.Serialize(new { error = ex.Message });
            dbContext.BulkOperationLogs.Update(log);
            await dbContext.SaveChangesAsync();

            logger.LogError(ex, "Bulk stock import failed for file {FileName}", fileName);
            return new ErrorDataResult<BulkImportResultDto>(null!, $"Stok güncelleme sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<IDataResult<byte[]>> ExportProductsAsync(ExportFilterDto filter)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var query = dbContext.ProductVariants
                .Include(pv => pv.Product)
                    .ThenInclude(p => p.Brand)
                .Include(pv => pv.Product)
                    .ThenInclude(p => p.Category)
                .Where(pv => !pv.Product.IsDeleted);

            if (filter.CategoryId.HasValue)
                query = query.Where(pv => pv.Product.CategoryId == filter.CategoryId.Value);

            if (filter.BrandId.HasValue)
                query = query.Where(pv => pv.Product.BrandId == filter.BrandId.Value);

            if (!filter.IncludeDeleted)
                query = query.Where(pv => !pv.IsDeleted);

            if (filter.DateFrom.HasValue)
                query = query.Where(pv => pv.Product.CreatedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(pv => pv.Product.CreatedAt <= filter.DateTo.Value);

            var data = await query
                .OrderBy(pv => pv.Barcode)
                .Select(pv => new
                {
                    pv.Barcode,
                    pv.Product.Title,
                    pv.Product.StockCode,
                    pv.ListPrice,
                    pv.SalePrice,
                    pv.CostPrice,
                    pv.VatRate,
                    CategoryName = pv.Product.Category.Name,
                    BrandName = pv.Product.Brand != null ? pv.Product.Brand.Name : ""
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var headers = new[] { "Barkod", "Ürün Adı", "Stok Kodu", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı", "KDV Oranı", "Kategori", "Marka" };

            WriteToExcelSheets(workbook, "Ürünler", headers, data, (ws, rowNum, row) =>
            {
                ws.Cell(rowNum, 1).Value = row.Barcode ?? "";
                ws.Cell(rowNum, 2).Value = row.Title;
                ws.Cell(rowNum, 3).Value = row.StockCode ?? "";
                ws.Cell(rowNum, 4).Value = row.ListPrice;
                ws.Cell(rowNum, 5).Value = row.SalePrice;
                ws.Cell(rowNum, 6).Value = row.CostPrice;
                ws.Cell(rowNum, 7).Value = row.VatRate;
                ws.Cell(rowNum, 8).Value = row.CategoryName;
                ws.Cell(rowNum, 9).Value = row.BrandName;
            });

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return new SuccessDataResult<byte[]>(ms.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Product export failed");
            return new ErrorDataResult<byte[]>([], $"Dışa aktarma sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<IDataResult<byte[]>> ExportPricesAsync(ExportFilterDto filter)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var query = dbContext.ProductVariants
                .Include(pv => pv.Product)
                .Where(pv => !pv.Product.IsDeleted);

            if (filter.CategoryId.HasValue)
                query = query.Where(pv => pv.Product.CategoryId == filter.CategoryId.Value);

            if (filter.BrandId.HasValue)
                query = query.Where(pv => pv.Product.BrandId == filter.BrandId.Value);

            if (!filter.IncludeDeleted)
                query = query.Where(pv => !pv.IsDeleted);

            if (filter.DateFrom.HasValue)
                query = query.Where(pv => pv.Product.CreatedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(pv => pv.Product.CreatedAt <= filter.DateTo.Value);

            var data = await query
                .OrderBy(pv => pv.Barcode)
                .Select(pv => new { pv.Barcode, pv.ListPrice, pv.SalePrice, pv.CostPrice })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var headers = new[] { "Barkod", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı" };

            WriteToExcelSheets(workbook, "Fiyatlar", headers, data, (ws, rowNum, row) =>
            {
                ws.Cell(rowNum, 1).Value = row.Barcode ?? "";
                ws.Cell(rowNum, 2).Value = row.ListPrice;
                ws.Cell(rowNum, 3).Value = row.SalePrice;
                ws.Cell(rowNum, 4).Value = row.CostPrice;
            });

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return new SuccessDataResult<byte[]>(ms.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Price export failed");
            return new ErrorDataResult<byte[]>([], $"Fiyat dışa aktarma sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<IDataResult<byte[]>> ExportStockAsync(ExportFilterDto filter)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var query = dbContext.BranchOfficeStocks
                .Include(s => s.ProductVariant!)
                    .ThenInclude(pv => pv.Product)
                .Include(s => s.BranchOffice)
                .Where(s => s.ProductVariant != null && !s.ProductVariant.IsDeleted && !s.ProductVariant.Product.IsDeleted);

            if (filter.BranchOfficeId.HasValue)
                query = query.Where(s => s.BranchOfficeId == filter.BranchOfficeId.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(s => s.ProductVariant!.Product.CategoryId == filter.CategoryId.Value);

            if (filter.BrandId.HasValue)
                query = query.Where(s => s.ProductVariant!.Product.BrandId == filter.BrandId.Value);

            if (filter.DateFrom.HasValue)
                query = query.Where(s => s.ProductVariant!.Product.CreatedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(s => s.ProductVariant!.Product.CreatedAt <= filter.DateTo.Value);

            var data = await query
                .OrderBy(s => s.ProductVariant!.Barcode)
                .Select(s => new
                {
                    s.ProductVariant!.Barcode,
                    s.BranchOfficeId,
                    BranchOfficeName = s.BranchOffice.Name,
                    s.FirstTotalStock,
                    s.CurrentStock
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var headers = new[] { "Barkod", "Şube ID", "Şube Adı", "Toplam Stok", "Güncel Stok" };

            WriteToExcelSheets(workbook, "Stok", headers, data, (ws, rowNum, row) =>
            {
                ws.Cell(rowNum, 1).Value = row.Barcode ?? "";
                ws.Cell(rowNum, 2).Value = row.BranchOfficeId;
                ws.Cell(rowNum, 3).Value = row.BranchOfficeName;
                ws.Cell(rowNum, 4).Value = row.FirstTotalStock;
                ws.Cell(rowNum, 5).Value = row.CurrentStock;
            });

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return new SuccessDataResult<byte[]>(ms.ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stock export failed");
            return new ErrorDataResult<byte[]>([], $"Stok dışa aktarma sırasında hata oluştu: {ex.Message}");
        }
    }

    public Task<IDataResult<byte[]>> GetImportTemplateAsync(BulkOperationType type)
    {
        try
        {
            using var workbook = new XLWorkbook();

            string[] headers = type switch
            {
                BulkOperationType.ProductImport => ["Barkod", "Ürün Adı", "Stok Kodu", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı", "KDV Oranı", "Kategori", "Marka"],
                BulkOperationType.PriceImport => ["Barkod", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı"],
                BulkOperationType.StockImport => ["Barkod", "Şube ID", "Stok Miktarı"],
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            var sheetName = type switch
            {
                BulkOperationType.ProductImport => "Ürün Şablonu",
                BulkOperationType.PriceImport => "Fiyat Şablonu",
                BulkOperationType.StockImport => "Stok Şablonu",
                _ => "Şablon"
            };

            var ws = workbook.AddWorksheet(sheetName);
            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return Task.FromResult<IDataResult<byte[]>>(new SuccessDataResult<byte[]>(ms.ToArray()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Template generation failed for type {Type}", type);
            return Task.FromResult<IDataResult<byte[]>>(
                new ErrorDataResult<byte[]>([], $"Şablon oluşturulurken hata oluştu: {ex.Message}"));
        }
    }

    private static void WriteToExcelSheets<T>(
        XLWorkbook workbook, string sheetName, string[] headers,
        List<T> data, Action<IXLWorksheet, int, T> writeRow)
    {
        const int maxRowsPerSheet = 1_048_575; // Excel limit minus header row

        var totalSheets = (int)Math.Ceiling((double)data.Count / maxRowsPerSheet);
        if (totalSheets == 0) totalSheets = 1;

        for (var sheetIdx = 0; sheetIdx < totalSheets; sheetIdx++)
        {
            var name = sheetIdx == 0 ? sheetName : $"{sheetName} ({sheetIdx + 1})";
            var ws = workbook.AddWorksheet(name);

            for (var h = 0; h < headers.Length; h++)
                ws.Cell(1, h + 1).Value = headers[h];
            ws.Row(1).Style.Font.Bold = true;

            var start = sheetIdx * maxRowsPerSheet;
            var count = Math.Min(maxRowsPerSheet, data.Count - start);

            for (var i = 0; i < count; i++)
                writeRow(ws, i + 2, data[start + i]);

            if (data.Count <= 50_000)
                ws.Columns().AdjustToContents();
        }
    }

    public Task<IDataResult<ImportValidationPreviewDto>> ValidateImportAsync(Stream excelStream, BulkOperationType operationType)
    {
        return operationType switch
        {
            BulkOperationType.ProductImport => ValidateProductImportAsync(excelStream),
            BulkOperationType.PriceImport => ValidatePriceImportAsync(excelStream),
            BulkOperationType.StockImport => ValidateStockImportAsync(excelStream),
            _ => Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, "Geçersiz işlem tipi."))
        };
    }

    private Task<IDataResult<ImportValidationPreviewDto>> ValidateProductImportAsync(Stream excelStream)
    {
        var parseResult = excelParser.ParseProductImport(excelStream);
        if (!parseResult.Success)
            return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, parseResult.Message!));

        var rows = parseResult.Data;
        if (rows.Count == 0)
            return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, "Excel dosyası boş veya veri satırı bulunamadı."));

        var validationErrors = importValidator.ValidateProductRows(rows);
        var invalidBarcodes = validationErrors.Select(e => e.Barcode).ToHashSet();
        var validRows = rows.Where(r => !invalidBarcodes.Contains(r.Barcode)).ToList();

        // Check duplicates
        var seenBarcodes = new HashSet<string>();
        var duplicateErrors = new List<BulkImportRowErrorDto>();
        var deduplicatedCount = 0;

        foreach (var row in validRows)
        {
            if (!seenBarcodes.Add(row.Barcode))
            {
                duplicateErrors.Add(new BulkImportRowErrorDto(row.RowNumber, row.Barcode, "Dosya içinde tekrarlayan barkod."));
                continue;
            }
            deduplicatedCount++;
        }

        var allErrors = validationErrors.Concat(duplicateErrors).OrderBy(e => e.RowNumber).ToList();

        var preview = new ImportValidationPreviewDto(
            TotalRows: rows.Count,
            ValidRows: deduplicatedCount,
            InvalidRows: rows.Count - deduplicatedCount,
            Errors: allErrors);

        return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
            new SuccessDataResult<ImportValidationPreviewDto>(preview));
    }

    private Task<IDataResult<ImportValidationPreviewDto>> ValidatePriceImportAsync(Stream excelStream)
    {
        var parseResult = excelParser.ParsePriceImport(excelStream);
        if (!parseResult.Success)
            return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, parseResult.Message!));

        var rows = parseResult.Data;
        if (rows.Count == 0)
            return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, "Excel dosyası boş veya veri satırı bulunamadı."));

        var validationErrors = importValidator.ValidatePriceRows(rows);
        var invalidBarcodes = validationErrors.Select(e => e.Barcode).ToHashSet();
        var validCount = rows.Count(r => !invalidBarcodes.Contains(r.Barcode));

        var preview = new ImportValidationPreviewDto(
            TotalRows: rows.Count,
            ValidRows: validCount,
            InvalidRows: rows.Count - validCount,
            Errors: validationErrors);

        return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
            new SuccessDataResult<ImportValidationPreviewDto>(preview));
    }

    private Task<IDataResult<ImportValidationPreviewDto>> ValidateStockImportAsync(Stream excelStream)
    {
        var parseResult = excelParser.ParseStockImport(excelStream);
        if (!parseResult.Success)
            return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, parseResult.Message!));

        var rows = parseResult.Data;
        if (rows.Count == 0)
            return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
                new ErrorDataResult<ImportValidationPreviewDto>(null!, "Excel dosyası boş veya veri satırı bulunamadı."));

        var validationErrors = importValidator.ValidateStockRows(rows);
        var invalidBarcodes = validationErrors.Select(e => e.Barcode).ToHashSet();
        var validCount = rows.Count(r => !invalidBarcodes.Contains(r.Barcode));

        var preview = new ImportValidationPreviewDto(
            TotalRows: rows.Count,
            ValidRows: validCount,
            InvalidRows: rows.Count - validCount,
            Errors: validationErrors);

        return Task.FromResult<IDataResult<ImportValidationPreviewDto>>(
            new SuccessDataResult<ImportValidationPreviewDto>(preview));
    }

    public async Task<IDataResult<BulkOperationLog>> GetOperationLogAsync(long id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var log = await dbContext.BulkOperationLogs.FirstOrDefaultAsync(x => x.Id == id);
        if (log is null)
            return new ErrorDataResult<BulkOperationLog>(null!, "İşlem kaydı bulunamadı.");

        return new SuccessDataResult<BulkOperationLog>(log);
    }

    public async Task<IDataResult<List<BulkOperationLog>>> GetRecentOperationsAsync(int count = 20)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var logs = await dbContext.BulkOperationLogs
            .OrderByDescending(x => x.StartedAt)
            .Take(count)
            .ToListAsync();

        return new SuccessDataResult<List<BulkOperationLog>>(logs);
    }
}
