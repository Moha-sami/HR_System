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

    public static IReadOnlyList<(TimeSpan Start, TimeSpan End, int EmployeeId)> GetOverlappingBlocksForSameEmployee(
        IEnumerable<(TimeSpan Start, TimeSpan End, int EmployeeId)> blocks,
        TimeSpan templateStart)
    {
        var overlapping = new List<(TimeSpan Start, TimeSpan End, int EmployeeId)>();
        foreach (var group in blocks.GroupBy(b => b.EmployeeId))
        {
            CollectOverlappingBlocks(group.ToList(), templateStart, overlapping);
        }

        return overlapping;
    }

    public static bool HasOverlappingBlocksForSameEmployee(
        IEnumerable<(TimeSpan Start, TimeSpan End, int EmployeeId)> blocks,
        TimeSpan templateStart)
    {
        return GetOverlappingBlocksForSameEmployee(blocks, templateStart).Count != 0;
    }

    private static void CollectOverlappingBlocks(
        IReadOnlyList<(TimeSpan Start, TimeSpan End, int EmployeeId)> group,
        TimeSpan templateStart,
        List<(TimeSpan Start, TimeSpan End, int EmployeeId)> accumulator)
    {
        var ordered = group
            .Select(b => (Block: b, Interval: ToOffsetInterval(b.Start, b.End, templateStart)))
            .OrderBy(x => x.Interval.Offset)
            .ToList();

        var overlappingIndexes = new HashSet<int>();
        var openIndexes = new List<int>();

        for (var i = 0; i < ordered.Count; i++)
        {
            var currentOffset = ordered[i].Interval.Offset;
            for (var k = openIndexes.Count - 1; k >= 0; k--)
            {
                if (ordered[openIndexes[k]].Interval.End <= currentOffset + 1e-9)
                {
                    openIndexes.RemoveAt(k);
                }
                else
                {
                    overlappingIndexes.Add(openIndexes[k]);
                    overlappingIndexes.Add(i);
                }
            }

            openIndexes.Add(i);
        }

        foreach (var index in overlappingIndexes.OrderBy(x => x))
        {
            accumulator.Add(ordered[index].Block);
        }
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
