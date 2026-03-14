using System.Text;
using Entegrasyon.Entity.Labels;
using Entegrasyon.PrintAgent.Contracts.Labels;

namespace Entegrasyon.Business.Labels;

public class ZplLabelGenerator : ILabelGenerator
{
    public string GenerateProductBarcode(ProductBarcodeLabelData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("^XA"); // Format başlangıcı

        // Ürün adı
        sb.AppendLine("^FO50,40^A0N,30,30^FD" + SanitizeZpl(TruncateText(data.ProductTitle, 40)) + "^FS");

        // Varyant bilgisi
        if (!string.IsNullOrWhiteSpace(data.VariantInfo))
            sb.AppendLine("^FO50,80^A0N,22,22^FD" + SanitizeZpl(data.VariantInfo) + "^FS");

        // Code 128 Barkod
        sb.AppendLine("^FO50,120^BCN,80,Y,N,N^FD" + data.Barcode + "^FS");

        // Fiyat
        sb.AppendLine("^FO50,280^A0N,40,40^FD" + $"{data.Price:N2} {data.CurrencySymbol}" + "^FS");

        sb.AppendLine("^XZ"); // Format sonu
        return sb.ToString();
    }

    public string GenerateShelfLabel(ShelfLabelData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("^XA");

        // Ürün adı
        sb.AppendLine("^FO30,30^A0N,24,24^FD" + SanitizeZpl(TruncateText(data.ProductTitle, 30)) + "^FS");

        // Barkod
        sb.AppendLine("^FO30,65^BCN,60,Y,N,N^FD" + data.Barcode + "^FS");

        // Fiyat (büyük)
        sb.AppendLine("^FO30,180^A0N,48,48^FD" + $"{data.Price:N2} {data.CurrencySymbol}" + "^FS");

        sb.AppendLine("^XZ");
        return sb.ToString();
    }

    public string GenerateFromTemplate(List<LabelElement> elements, int widthDots, int heightDots, Dictionary<string, string> fieldValues)
    {
        var sb = new StringBuilder();
        sb.AppendLine("^XA");

        // Etiket boyutu
        sb.AppendLine($"^LL{heightDots}");
        sb.AppendLine($"^PW{widthDots}");

        foreach (var el in elements)
        {
            var x = (int)el.X;
            var y = (int)el.Y;

            switch (el.ElementType)
            {
                case "Barcode":
                {
                    var value = fieldValues.GetValueOrDefault("Barcode", "0000000000");
                    var height = Math.Max((int)el.Height, 20);
                    sb.AppendLine($"^FO{x},{y}^BCN,{height},Y,N,N^FD{SanitizeZpl(value)}^FS");
                    break;
                }
                case "ListPrice":
                {
                    var value = fieldValues.GetValueOrDefault("ListPrice", "0,00 ₺");
                    var fontSize = el.FontSize > 0 ? el.FontSize : 24;
                    var fontWidth = el.Bold ? fontSize + 4 : fontSize;
                    sb.AppendLine($"^FO{x},{y}^A0N,{fontSize},{fontWidth}^FD{SanitizeZpl(value)}^FS");
                    // Üstü çizili çizgi
                    var lineY = y + fontSize / 2;
                    var textWidth = (int)(value.Length * fontSize * 0.6);
                    sb.AppendLine($"^FO{x},{lineY}^GB{textWidth},2,2^FS");
                    break;
                }
                case "Line":
                {
                    var w = Math.Max((int)el.Width, 10);
                    sb.AppendLine($"^FO{x},{y}^GB{w},2,2^FS");
                    break;
                }
                default:
                {
                    // Title, VariantInfo, SalePrice, CustomText
                    var value = el.ElementType == "CustomText"
                        ? el.CustomText ?? ""
                        : fieldValues.GetValueOrDefault(el.ElementType, "");

                    var fontSize = el.FontSize > 0 ? el.FontSize : 24;
                    var fontWidth = el.Bold ? fontSize + 4 : fontSize;
                    sb.AppendLine($"^FO{x},{y}^A0N,{fontSize},{fontWidth}^FD{SanitizeZpl(value)}^FS");
                    break;
                }
            }
        }

        sb.AppendLine("^XZ");
        return sb.ToString();
    }

    private static string TruncateText(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

    private static string SanitizeZpl(string text)
        => text.Replace("^", "").Replace("~", "");
}
