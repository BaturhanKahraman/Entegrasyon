namespace Entegrasyon.Entity.Dtos.Label;

public record PrintJobDto(
    string ZplContent,
    byte[] RawBytes,
    string PrinterLanguage,
    string JobLabel);
