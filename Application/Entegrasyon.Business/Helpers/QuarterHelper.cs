namespace Entegrasyon.Business.Helpers;

/// <summary>Takvim çeyreği (Q1=Oca-Mar ... Q4=Eki-Ara) aralık hesabı.</summary>
public static class QuarterHelper
{
    /// <summary>Verilen tarihin içinde bulunduğu çeyreğin ilk ve son günü.</summary>
    public static (DateOnly Start, DateOnly End) GetQuarterRange(DateOnly date)
    {
        var quarter = (date.Month - 1) / 3;          // 0..3
        var startMonth = quarter * 3 + 1;            // 1, 4, 7, 10
        var start = new DateOnly(date.Year, startMonth, 1);
        var end = start.AddMonths(3).AddDays(-1);    // çeyreğin son günü
        return (start, end);
    }

    /// <summary>Verilen tarihin içinde bulunduğu çeyreğin bir öncekinin aralığı.</summary>
    public static (DateOnly Start, DateOnly End) GetPreviousQuarterRange(DateOnly date)
    {
        var current = GetQuarterRange(date);
        return GetQuarterRange(current.Start.AddDays(-1));
    }
}
