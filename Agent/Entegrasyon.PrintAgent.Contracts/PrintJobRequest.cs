namespace Entegrasyon.PrintAgent.Contracts;

public record PrintJobRequest(
    string ZplContent,
    byte[] RawBytes,
    string TargetPrinter,
    int Copies = 1,
    string JobLabel = null,
    PrinterLanguage Language = PrinterLanguage.ZPL);
