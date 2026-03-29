using ClosedXML.Excel;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public class ExcelParser : IExcelParser
{
    private static readonly string[] ProductHeaders = ["Barkod", "Ürün Adı", "Stok Kodu", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı", "KDV Oranı", "Kategori", "Marka"];
    private static readonly string[] PriceHeaders = ["Barkod", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı"];
    private static readonly string[] StockHeaders = ["Barkod", "Şube ID", "Stok Miktarı"];

    public IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            var headerValidation = ValidateHeaders(worksheet, ProductHeaders);
            if (!headerValidation.Success)
                return new ErrorDataResult<List<ProductImportRow>>([], headerValidation.Message!);

            var rows = new List<ProductImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var i = 2; i <= lastRow; i++)
            {
                var row = worksheet.Row(i);
                if (IsRowEmpty(row, ProductHeaders.Length))
                    continue;

                var barcode = row.Cell(1).GetString().Trim();
                var title = row.Cell(2).GetString().Trim();
                var stockCode = row.Cell(3).GetString().Trim();
                var listPrice = ParseDecimal(row.Cell(4));
                var salePrice = ParseDecimal(row.Cell(5));
                var costPrice = ParseDecimal(row.Cell(6));
                var vatRate = ParseDecimal(row.Cell(7));
                var category = row.Cell(8).GetString().Trim();
                var brand = row.Cell(9).GetString().Trim();

                rows.Add(new ProductImportRow(
                    i, barcode, title,
                    string.IsNullOrWhiteSpace(stockCode) ? null : stockCode,
                    listPrice, salePrice, costPrice, vatRate,
                    string.IsNullOrWhiteSpace(category) ? null : category,
                    string.IsNullOrWhiteSpace(brand) ? null : brand));
            }

            return new SuccessDataResult<List<ProductImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<ProductImportRow>>([], $"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            var headerValidation = ValidateHeaders(worksheet, PriceHeaders);
            if (!headerValidation.Success)
                return new ErrorDataResult<List<PriceImportRow>>([], headerValidation.Message!);

            var rows = new List<PriceImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var i = 2; i <= lastRow; i++)
            {
                var row = worksheet.Row(i);
                if (IsRowEmpty(row, PriceHeaders.Length))
                    continue;

                rows.Add(new PriceImportRow(
                    i,
                    row.Cell(1).GetString().Trim(),
                    ParseDecimal(row.Cell(2)),
                    ParseDecimal(row.Cell(3)),
                    ParseDecimal(row.Cell(4))));
            }

            return new SuccessDataResult<List<PriceImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<PriceImportRow>>([], $"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<StockImportRow>> ParseStockImport(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            var headerValidation = ValidateHeaders(worksheet, StockHeaders);
            if (!headerValidation.Success)
                return new ErrorDataResult<List<StockImportRow>>([], headerValidation.Message!);

            var rows = new List<StockImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var i = 2; i <= lastRow; i++)
            {
                var row = worksheet.Row(i);
                if (IsRowEmpty(row, StockHeaders.Length))
                    continue;

                rows.Add(new StockImportRow(
                    i,
                    row.Cell(1).GetString().Trim(),
                    ParseInt(row.Cell(2)),
                    ParseInt(row.Cell(3))));
            }

            return new SuccessDataResult<List<StockImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<StockImportRow>>([], $"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    private static IResult ValidateHeaders(IXLWorksheet worksheet, string[] expectedHeaders)
    {
        var headerRow = worksheet.Row(1);
        var missingHeaders = new List<string>();

        for (var i = 0; i < expectedHeaders.Length; i++)
        {
            var actual = headerRow.Cell(i + 1).GetString().Trim();
            if (!string.Equals(actual, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                missingHeaders.Add(expectedHeaders[i]);
        }

        if (missingHeaders.Count > 0)
            return new ErrorResult($"Eksik veya hatalı sütun başlıkları: {string.Join(", ", missingHeaders)}");

        return new SuccessResult();
    }

    private static bool IsRowEmpty(IXLRow row, int columnCount)
    {
        for (var i = 1; i <= columnCount; i++)
        {
            if (!string.IsNullOrWhiteSpace(row.Cell(i).GetString()))
                return false;
        }
        return true;
    }

    private static decimal ParseDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var value))
            return value;
        if (decimal.TryParse(cell.GetString().Trim(), out var parsed))
            return parsed;
        return 0;
    }

    private static int ParseInt(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var value))
            return value;
        if (int.TryParse(cell.GetString().Trim(), out var parsed))
            return parsed;
        return 0;
    }
}
