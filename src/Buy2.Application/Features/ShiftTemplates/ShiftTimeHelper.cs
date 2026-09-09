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

    public static bool HasOverlappingBlocks(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> blocks,
        TimeSpan templateStart)
    {
        if (blocks.Count < 2)
        {
            return false;
        }

        var normalized = blocks
            .Select(b => ToOffsetInterval(b.Start, b.End, templateStart))
            .OrderBy(x => x.Offset)
            .ToList();

        for (var i = 1; i < normalized.Count; i++)
        {
            if (normalized[i].Offset < normalized[i - 1].End - 1e-9)
            {
                return true;
            }
        }

        return false;
    }

    private static (double Offset, double End) ToOffsetInterval(
        TimeSpan blockStart,
        TimeSpan blockEnd,
        TimeSpan templateStart)
    {
        var offset = (blockStart.TotalMinutes - templateStart.TotalMinutes + DayMinutes) % DayMinutes;
        return (offset, offset + DurationMinutes(blockStart, blockEnd));
    }
}
