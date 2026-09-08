using System.Globalization;

namespace Buy2.Application.Features.ShiftTemplates;

public static class ShiftTimeHelper
{
    private static readonly string[] TimeFormats = ["h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt"];
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private const double DayMinutes = 24 * 60;

    public static bool TryParseTime(string? value, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var input = value.Trim().ToUpperInvariant();
        if (DateTime.TryParseExact(input, TimeFormats, Invariant, DateTimeStyles.None, out var parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        return false;
    }

    public static string FormatTime(TimeSpan time)
    {
        return DateTime.Today.Add(time).ToString("hh:mm tt", Invariant);
    }

    public static string FormatDate(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd", Invariant);
    }

    public static string FormatDate(DateTimeOffset dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd", Invariant);
    }

    /// <summary>
    /// Checks that a block interval fits inside the template interval.
    /// Overnight intervals (end &lt;= start) wrap past midnight and are supported
    /// for both the template and the block.
    /// </summary>
    public static bool IsWithinTemplate(TimeSpan blockStart, TimeSpan blockEnd, TimeSpan templateStart, TimeSpan templateEnd)
    {
        double templateDuration = DurationMinutes(templateStart, templateEnd);
        if (templateDuration <= 0 || templateDuration >= DayMinutes)
        {
            return false;
        }

        double blockDuration = DurationMinutes(blockStart, blockEnd);
        if (blockDuration <= 0 || blockDuration > templateDuration)
        {
            return false;
        }

        double blockOffset = (blockStart.TotalMinutes - templateStart.TotalMinutes + DayMinutes) % DayMinutes;
        return blockOffset + blockDuration <= templateDuration + 1e-9;
    }

    private static double DurationMinutes(TimeSpan start, TimeSpan end)
    {
        return (end.TotalMinutes - start.TotalMinutes + DayMinutes) % DayMinutes;
    }
}
