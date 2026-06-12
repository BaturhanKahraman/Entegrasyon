namespace Entegrasyon.Entity.Dtos.Reports;

/// <summary>KDV beyan özeti — bir KDV oranı için matrah + KDV toplamı (kesilen fatura satırlarından).</summary>
public record VatDeclarationLineDto(int VatRate, decimal TaxBase, decimal VatAmount, int LineCount);

/// <summary>
/// KDV Beyan Özeti: belirli dönemde KESİLEN (Sent/Accepted) faturaların satırları,
/// KDV oranına göre gruplanmış matrah + KDV. Muhasebe beyanı için.
/// </summary>
public record VatDeclarationDto(
    IReadOnlyList<VatDeclarationLineDto> Lines,
    decimal TotalTaxBase,
    decimal TotalVat);
