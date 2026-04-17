namespace Entegrasyon.Business.Utilities;

public static class TurkeyTime
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTimeOffset Now
        => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZone);

    public static DateTimeOffset StartOfToday
    {
        get
        {
            var localDate = Now.Date;
            var offset = TimeZone.GetUtcOffset(localDate);
            return new DateTimeOffset(localDate, offset);
        }
    }

    public static DateTimeOffset StartOfDay(DateTimeOffset instant)
    {
        var localDate = TimeZoneInfo.ConvertTime(instant, TimeZone).Date;
        var offset = TimeZone.GetUtcOffset(localDate);
        return new DateTimeOffset(localDate, offset);
    }

    public static DateTimeOffset ToUtc(DateTimeOffset instant)
        => instant.ToUniversalTime();
}
