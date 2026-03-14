using System.Text;
using Entegrasyon.PrintAgent.Contracts.Labels;

namespace Entegrasyon.Business.Labels;

public class EscPosReceiptGenerator : IReceiptGenerator
{
    // ESC/POS komutları
    private static readonly byte[] Initialize = [0x1B, 0x40]; // ESC @
    private static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01]; // ESC a 1
    private static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00]; // ESC a 0
    private static readonly byte[] AlignRight = [0x1B, 0x61, 0x02]; // ESC a 2
    private static readonly byte[] BoldOn = [0x1B, 0x45, 0x01]; // ESC E 1
    private static readonly byte[] BoldOff = [0x1B, 0x45, 0x00]; // ESC E 0
    private static readonly byte[] DoubleSizeOn = [0x1D, 0x21, 0x11]; // GS ! 0x11
    private static readonly byte[] DoubleSizeOff = [0x1D, 0x21, 0x00]; // GS ! 0x00
    private static readonly byte[] CutPaper = [0x1D, 0x56, 0x00]; // GS V 0 (full cut)
    private static readonly byte[] FeedLines = [0x1B, 0x64, 0x04]; // ESC d 4

    private const int LineWidth = 42; // 80mm yazıcı standart karakter genişliği

    public byte[] GenerateSaleReceipt(SaleReceiptData data)
    {
        using var ms = new MemoryStream();
        var encoding = Encoding.GetEncoding("iso-8859-9"); // Türkçe karakter desteği

        ms.Write(Initialize);

        // Mağaza başlığı
        ms.Write(AlignCenter);
        ms.Write(BoldOn);
        ms.Write(DoubleSizeOn);
        WriteText(ms, data.StoreName, encoding);
        ms.Write(DoubleSizeOff);
        ms.Write(BoldOff);
        WriteText(ms, data.StoreAddress, encoding);
        WriteText(ms, $"VKN: {data.TaxId}", encoding);
        WriteText(ms, new string('-', LineWidth), encoding);

        // Tarih ve kasiyer
        ms.Write(AlignLeft);
        WriteText(ms, $"Tarih: {data.SaleDate:dd.MM.yyyy HH:mm}", encoding);
        WriteText(ms, $"Kasiyer: {data.CashierName}", encoding);

        if (!string.IsNullOrWhiteSpace(data.CustomerName))
            WriteText(ms, $"Musteri: {data.CustomerName}", encoding);

        WriteText(ms, new string('-', LineWidth), encoding);

        // Ürünler
        foreach (var item in data.Items)
        {
            WriteText(ms, item.ProductName, encoding);
            var detail = $"  {item.Quantity} x {item.UnitPrice:N2}";
            var total = $"{item.LineTotal:N2}";
            var padding = LineWidth - detail.Length - total.Length;
            WriteText(ms, detail + new string(' ', Math.Max(1, padding)) + total, encoding);
        }

        WriteText(ms, new string('-', LineWidth), encoding);

        // Toplamlar
        WriteLineWithAmount(ms, "Ara Toplam:", data.SubTotal, encoding);

        if (data.Discount > 0)
            WriteLineWithAmount(ms, "Indirim:", -data.Discount, encoding);

        ms.Write(BoldOn);
        ms.Write(DoubleSizeOn);
        WriteLineWithAmount(ms, "TOPLAM:", data.Total, encoding);
        ms.Write(DoubleSizeOff);
        ms.Write(BoldOff);

        WriteText(ms, $"Odeme: {data.PaymentMethod}", encoding);
        WriteText(ms, new string('-', LineWidth), encoding);

        // Alt bilgi
        ms.Write(AlignCenter);
        WriteText(ms, "Bizi tercih ettiginiz icin tesekkurler!", encoding);

        ms.Write(FeedLines);
        ms.Write(CutPaper);

        return ms.ToArray();
    }

    private static void WriteText(MemoryStream ms, string text, Encoding encoding)
    {
        var bytes = encoding.GetBytes(text + "\n");
        ms.Write(bytes);
    }

    private static void WriteLineWithAmount(MemoryStream ms, string label, decimal amount, Encoding encoding)
    {
        ms.Write(AlignLeft);
        var amountText = $"{amount:N2} TL";
        var padding = LineWidth - label.Length - amountText.Length;
        WriteText(ms, label + new string(' ', Math.Max(1, padding)) + amountText, encoding);
    }
}
