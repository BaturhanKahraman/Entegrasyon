using System.Text;

namespace Entegrasyon.Business.Concrete.BulkOperations;

public static class CsvWriter
{
    private const char Separator = ';';

    public static byte[] WriteCsv(string[] headers, List<string[]> rows)
    {
        using var ms = new MemoryStream();
        // Write UTF-8 BOM for Turkish character support in Excel
        ms.Write(Encoding.UTF8.GetPreamble());

        using var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true);

        // Header
        writer.WriteLine(string.Join(Separator, headers));

        // Data rows
        foreach (var row in rows)
        {
            var escapedCells = row.Select(EscapeField);
            writer.WriteLine(string.Join(Separator, escapedCells));
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Contains(Separator) || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
