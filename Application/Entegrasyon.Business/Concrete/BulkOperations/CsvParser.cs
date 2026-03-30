using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public class CsvParser : ICsvParser
{
    private const char Separator = ';';

    private static readonly string[] ProductHeaders = ["Barkod", "Ürün Adı", "Stok Kodu", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı", "KDV Oranı", "Kategori", "Marka"];
    private static readonly string[] PriceHeaders = ["Barkod", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı"];
    private static readonly string[] StockHeaders = ["Barkod", "Şube ID", "Stok Miktarı"];

    public IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream)
    {
        try
        {
            var lines = ReadLines(stream);
            if (lines.Count == 0)
                return new ErrorDataResult<List<ProductImportRow>>([], "CSV dosyası boş.");

            var headerValidation = ValidateHeaders(lines[0], ProductHeaders);
            if (!headerValidation.Success)
                return new ErrorDataResult<List<ProductImportRow>>([], headerValidation.Message!);

            var rows = new List<ProductImportRow>();
            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseLine(lines[i]);
                if (cells.Length < ProductHeaders.Length || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                var barcode = GetCell(cells, 0);
                var title = GetCell(cells, 1);
                var stockCode = GetCell(cells, 2);
                var listPrice = ParseDecimal(cells, 3);
                var salePrice = ParseDecimal(cells, 4);
                var costPrice = ParseDecimal(cells, 5);
                var vatRate = ParseDecimal(cells, 6);
                var category = GetCell(cells, 7);
                var brand = GetCell(cells, 8);

                rows.Add(new ProductImportRow(
                    i + 1, barcode, title,
                    string.IsNullOrWhiteSpace(stockCode) ? null : stockCode,
                    listPrice, salePrice, costPrice, vatRate,
                    string.IsNullOrWhiteSpace(category) ? null : category,
                    string.IsNullOrWhiteSpace(brand) ? null : brand));
            }

            return new SuccessDataResult<List<ProductImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<ProductImportRow>>([], $"CSV dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream)
    {
        try
        {
            var lines = ReadLines(stream);
            if (lines.Count == 0)
                return new ErrorDataResult<List<PriceImportRow>>([], "CSV dosyası boş.");

            var headerValidation = ValidateHeaders(lines[0], PriceHeaders);
            if (!headerValidation.Success)
                return new ErrorDataResult<List<PriceImportRow>>([], headerValidation.Message!);

            var rows = new List<PriceImportRow>();
            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseLine(lines[i]);
                if (cells.Length < PriceHeaders.Length || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                rows.Add(new PriceImportRow(
                    i + 1,
                    GetCell(cells, 0),
                    ParseDecimal(cells, 1),
                    ParseDecimal(cells, 2),
                    ParseDecimal(cells, 3)));
            }

            return new SuccessDataResult<List<PriceImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<PriceImportRow>>([], $"CSV dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<StockImportRow>> ParseStockImport(Stream stream)
    {
        try
        {
            var lines = ReadLines(stream);
            if (lines.Count == 0)
                return new ErrorDataResult<List<StockImportRow>>([], "CSV dosyası boş.");

            var headerValidation = ValidateHeaders(lines[0], StockHeaders);
            if (!headerValidation.Success)
                return new ErrorDataResult<List<StockImportRow>>([], headerValidation.Message!);

            var rows = new List<StockImportRow>();
            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseLine(lines[i]);
                if (cells.Length < StockHeaders.Length || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                rows.Add(new StockImportRow(
                    i + 1,
                    GetCell(cells, 0),
                    ParseInt(cells, 1),
                    ParseInt(cells, 2)));
            }

            return new SuccessDataResult<List<StockImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<StockImportRow>>([], $"CSV dosyası okunamadı: {ex.Message}");
        }
    }

    private static List<string> ReadLines(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }
        return lines;
    }

    private static string[] ParseLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var sb = new StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == Separator && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString().Trim());
        return result.ToArray();
    }

    private static IResult ValidateHeaders(string headerLine, string[] expectedHeaders)
    {
        var actual = ParseLine(headerLine);
        var missingHeaders = new List<string>();

        for (var i = 0; i < expectedHeaders.Length; i++)
        {
            if (i >= actual.Length || !string.Equals(actual[i], expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                missingHeaders.Add(expectedHeaders[i]);
        }

        if (missingHeaders.Count > 0)
            return new ErrorResult($"Eksik veya hatalı sütun başlıkları: {string.Join(", ", missingHeaders)}");

        return new SuccessResult();
    }

    private static string GetCell(string[] cells, int index)
        => index < cells.Length ? cells[index].Trim() : string.Empty;

    private static decimal ParseDecimal(string[] cells, int index)
    {
        var value = GetCell(cells, index);
        if (decimal.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        if (decimal.TryParse(value, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), out result))
            return result;
        return 0;
    }

    private static int ParseInt(string[] cells, int index)
    {
        var value = GetCell(cells, index);
        return int.TryParse(value, out var result) ? result : 0;
    }

    // ── Column Mapping Overloads ──

    public (List<string> Headers, List<List<string>> PreviewRows) ReadHeadersAndPreview(Stream stream, int previewRows = 5)
    {
        var lines = ReadLines(stream);
        if (lines.Count == 0)
            return ([], []);

        var headers = ParseLine(lines[0]).ToList();
        var preview = new List<List<string>>();

        for (var i = 1; i < Math.Min(lines.Count, 1 + previewRows); i++)
            preview.Add(ParseLine(lines[i]).ToList());

        return (headers, preview);
    }

    public IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream, Dictionary<string, string> columnMapping)
    {
        try
        {
            var lines = ReadLines(stream);
            if (lines.Count == 0)
                return new ErrorDataResult<List<ProductImportRow>>([], "CSV dosyası boş.");

            var colIndex = BuildColumnIndex(ParseLine(lines[0]), columnMapping);
            var rows = new List<ProductImportRow>();

            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseLine(lines[i]);
                var barcode = GetMappedCell(cells, colIndex, "Barkod");
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                var stockCode = GetMappedCell(cells, colIndex, "Stok Kodu");
                var category = GetMappedCell(cells, colIndex, "Kategori");
                var brand = GetMappedCell(cells, colIndex, "Marka");

                rows.Add(new ProductImportRow(i + 1, barcode,
                    GetMappedCell(cells, colIndex, "Ürün Adı"),
                    string.IsNullOrWhiteSpace(stockCode) ? null : stockCode,
                    GetMappedDecimal(cells, colIndex, "Liste Fiyatı"),
                    GetMappedDecimal(cells, colIndex, "Satış Fiyatı"),
                    GetMappedDecimal(cells, colIndex, "Maliyet Fiyatı"),
                    GetMappedDecimal(cells, colIndex, "KDV Oranı"),
                    string.IsNullOrWhiteSpace(category) ? null : category,
                    string.IsNullOrWhiteSpace(brand) ? null : brand));
            }

            return new SuccessDataResult<List<ProductImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<ProductImportRow>>([], $"CSV dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream, Dictionary<string, string> columnMapping)
    {
        try
        {
            var lines = ReadLines(stream);
            if (lines.Count == 0)
                return new ErrorDataResult<List<PriceImportRow>>([], "CSV dosyası boş.");

            var colIndex = BuildColumnIndex(ParseLine(lines[0]), columnMapping);
            var rows = new List<PriceImportRow>();

            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseLine(lines[i]);
                var barcode = GetMappedCell(cells, colIndex, "Barkod");
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                rows.Add(new PriceImportRow(i + 1, barcode,
                    GetMappedDecimal(cells, colIndex, "Liste Fiyatı"),
                    GetMappedDecimal(cells, colIndex, "Satış Fiyatı"),
                    GetMappedDecimal(cells, colIndex, "Maliyet Fiyatı")));
            }

            return new SuccessDataResult<List<PriceImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<PriceImportRow>>([], $"CSV dosyası okunamadı: {ex.Message}");
        }
    }

    public IDataResult<List<StockImportRow>> ParseStockImport(Stream stream, Dictionary<string, string> columnMapping)
    {
        try
        {
            var lines = ReadLines(stream);
            if (lines.Count == 0)
                return new ErrorDataResult<List<StockImportRow>>([], "CSV dosyası boş.");

            var colIndex = BuildColumnIndex(ParseLine(lines[0]), columnMapping);
            var rows = new List<StockImportRow>();

            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseLine(lines[i]);
                var barcode = GetMappedCell(cells, colIndex, "Barkod");
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                rows.Add(new StockImportRow(i + 1, barcode,
                    GetMappedInt(cells, colIndex, "Şube ID"),
                    GetMappedInt(cells, colIndex, "Stok Miktarı")));
            }

            return new SuccessDataResult<List<StockImportRow>>(rows);
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<List<StockImportRow>>([], $"CSV dosyası okunamadı: {ex.Message}");
        }
    }

    private static Dictionary<string, int> BuildColumnIndex(string[] headers, Dictionary<string, string> columnMapping)
    {
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
            headerIndex[headers[i].Trim()] = i;

        var result = new Dictionary<string, int>();
        foreach (var (systemField, excelColumn) in columnMapping)
        {
            if (headerIndex.TryGetValue(excelColumn, out var idx))
                result[systemField] = idx;
        }
        return result;
    }

    private static string GetMappedCell(string[] cells, Dictionary<string, int> colIndex, string field)
        => colIndex.TryGetValue(field, out var idx) ? GetCell(cells, idx) : string.Empty;

    private static decimal GetMappedDecimal(string[] cells, Dictionary<string, int> colIndex, string field)
        => colIndex.TryGetValue(field, out var idx) ? ParseDecimal(cells, idx) : 0;

    private static int GetMappedInt(string[] cells, Dictionary<string, int> colIndex, string field)
        => colIndex.TryGetValue(field, out var idx) ? ParseInt(cells, idx) : 0;
}
