namespace Entegrasyon.Entity.Dtos.Reports;

/// <summary>
/// Cohort retention matrisinin tek satırı. Müşteriler ilk-alışveriş ayına (Order.OrderDate MIN)
/// göre gruplanır; <see cref="RetentionPct"/>[i] = bu kohortun i. takip ayında tekrar alışveriş
/// yapan oranı (0-100 int). RetentionPct[0] tanımı gereği 100'dür (kohort ayının kendisi).
/// View 6 kolon (0..+5) gösterir; daha az ay varsa eksik kolonlar "·" görünür.
/// </summary>
public sealed record CustomerCohortRowDto(string CohortMonth, int Size, int[] RetentionPct);
