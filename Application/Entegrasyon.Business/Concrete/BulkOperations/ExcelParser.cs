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

    // ── Column Mapping Overloads ──

    public (List<string> Headers, List<List<string>> PreviewRows) ReadHeadersAndPreview(Stream stream, int previewRows = 5)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = new List<string>();
        var headerRow = worksheet.Row(1);
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var i = 1; i <= lastCol; i++)
            headers.Add(headerRow.Cell(i).GetString().Trim());

        var preview = new List<List<string>>();
        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 1, 1 + previewRows);

        for (var r = 2; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            var cells = new List<string>();
            for (var c = 1; c <= lastCol; c++)
                cells.Add(row.Cell(c).GetString().Trim());
            preview.Add(cells);
        }

        return (headers, preview);
    }

    public IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream, Dictionary<string, string> columnMapping)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var colIndex = BuildColumnIndex(worksheet, columnMapping);

            var rows = new List<ProductImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var i = 2; i <= lastRow; i++)
            {
                var row = worksheet.Row(i);
                var barcode = GetCellString(row, colIndex, "Barkod");
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                var title = GetCellString(row, colIndex, "Ürün Adı");
                var stockCode = GetCellString(row, colIndex, "Stok Kodu");

                rows.Add(new ProductImportRow(i, barcode, title,
                    string.IsNullOrWhiteSpace(stockCode) ? null : stockCode,
                    GetCellDecimal(row, colIndex, "Liste Fiyatı"),
                    GetCellDecimal(row, colIndex, "Satış Fiyatı"),
                    GetCellDecimal(row, colIndex, "Maliyet Fiyatı"),
                    GetCellDecimal(row, colIndex, "KDV Oranı"),
                    NullIfEmpty(GetCellString(row, colIndex, "Kategori")),
                    NullIfEmpty(GetCellString(row, colIndex, "Marka"))));
            }

            return new SuccessDataResult<List<ProductImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<ProductImportRow>>([], $"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream, Dictionary<string, string> columnMapping)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var colIndex = BuildColumnIndex(worksheet, columnMapping);

            var rows = new List<PriceImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var i = 2; i <= lastRow; i++)
            {
                var row = worksheet.Row(i);
                var barcode = GetCellString(row, colIndex, "Barkod");
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                rows.Add(new PriceImportRow(i, barcode,
                    GetCellDecimal(row, colIndex, "Liste Fiyatı"),
                    GetCellDecimal(row, colIndex, "Satış Fiyatı"),
                    GetCellDecimal(row, colIndex, "Maliyet Fiyatı")));
            }

            return new SuccessDataResult<List<PriceImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<PriceImportRow>>([], $"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<StockImportRow>> ParseStockImport(Stream stream, Dictionary<string, string> columnMapping)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var colIndex = BuildColumnIndex(worksheet, columnMapping);

            var rows = new List<StockImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var i = 2; i <= lastRow; i++)
            {
                var row = worksheet.Row(i);
                var barcode = GetCellString(row, colIndex, "Barkod");
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                rows.Add(new StockImportRow(i, barcode,
                    GetCellInt(row, colIndex, "Şube ID"),
                    GetCellInt(row, colIndex, "Stok Miktarı")));
            }

            return new SuccessDataResult<List<StockImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<StockImportRow>>([], $"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    /// <summary>
    /// Excel header satırından kolon index haritası oluşturur.
    /// columnMapping: {"SystemField": "ExcelColumnName"}
    /// Döndürür: {"SystemField": columnIndex (1-based)}
    /// </summary>
    private static Dictionary<string, int> BuildColumnIndex(IXLWorksheet worksheet, Dictionary<string, string> columnMapping)
    {
        var headerRow = worksheet.Row(1);
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i <= lastCol; i++)
            headerIndex[headerRow.Cell(i).GetString().Trim()] = i;

        var result = new Dictionary<string, int>();
        foreach (var (systemField, excelColumn) in columnMapping)
        {
            if (headerIndex.TryGetValue(excelColumn, out var idx))
                result[systemField] = idx;
        }

        return result;
    }

    private static string GetCellString(IXLRow row, Dictionary<string, int> colIndex, string field)
        => colIndex.TryGetValue(field, out var idx) ? row.Cell(idx).GetString().Trim() : string.Empty;

    private static decimal GetCellDecimal(IXLRow row, Dictionary<string, int> colIndex, string field)
        => colIndex.TryGetValue(field, out var idx) ? ParseDecimal(row.Cell(idx)) : 0;

    private static int GetCellInt(IXLRow row, Dictionary<string, int> colIndex, string field)
        => colIndex.TryGetValue(field, out var idx) ? ParseInt(row.Cell(idx)) : 0;

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    // ── Original Helpers ──

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
