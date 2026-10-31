namespace PTCenter.Infrastructure.Services;

/// <summary>Cairo calendar boundaries for UTC-stored records, including DST midnight transitions.</summary>
public static class ClinicCalendar
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
    public static (DateTime Start, DateTime End) UtcDay(DateTimeOffset instant)
    {
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);
        return (Start(day), Start(day.AddDays(1)));
    }
    private static DateTime Start(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (Zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        var offset = Zone.IsAmbiguousTime(local) ? Zone.GetAmbiguousTimeOffsets(local).Max() : Zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).UtcDateTime;
    }
}
