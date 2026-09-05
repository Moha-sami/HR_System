using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Points.Automation;

public record PeriodWindow(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

public static class CairoPeriodResolver
{
    public static TimeZoneInfo GetTimeZone(string? configuredId = null)
    {
        var candidates = new[] { configuredId, "Egypt Standard Time", "Africa/Cairo" }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var id in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id!);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    public static DateOnly TodayInCairo(DateTimeOffset utcNow, TimeZoneInfo cairoTimeZone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, cairoTimeZone).DateTime);
    }

    public static DateOnly ToCairoDate(DateTimeOffset utcValue, TimeZoneInfo cairoTimeZone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcValue, cairoTimeZone).DateTime);
    }

    public static int WindowLengthDays(AutomationPeriod period)
    {
        return period switch
        {
            AutomationPeriod.Daily => 1,
            AutomationPeriod.Weekly => 7,
            AutomationPeriod.BiWeekly => 14,
            _ => 1
        };
    }

    public static (DateOnly Start, DateOnly End)? TryGetNextDueWindow(
        AutomationPeriod period,
        DateOnly todayCairo,
        DateOnly? lastCompletedEndCairo)
    {
        if (period == AutomationPeriod.Monthly)
        {
            if (lastCompletedEndCairo is null)
            {
                var previousMonthStart = new DateOnly(todayCairo.Year, todayCairo.Month, 1).AddMonths(-1);
                var previousMonthEnd = previousMonthStart.AddMonths(1).AddDays(-1);

                if (previousMonthEnd >= todayCairo)
                {
                    return null;
                }

                return (previousMonthStart, previousMonthEnd);
            }

            // Anchor is the last evaluated day for this category regardless of the
            // period that produced it (handles Daily -> Monthly switches).
            // Next window always starts the day after the anchor so already
            // evaluated days are excluded.
            var nextStart = lastCompletedEndCairo.Value.AddDays(1);
            var nextMonthStart = new DateOnly(nextStart.Year, nextStart.Month, 1);
            DateOnly monthStart;
            DateOnly monthEnd;

            if (nextStart.Day == 1)
            {
                // Clean month boundary: evaluate the full calendar month.
                monthStart = nextMonthStart;
                monthEnd = monthStart.AddMonths(1).AddDays(-1);
            }
            else
            {
                // Period switch mid-month (e.g. Daily until day 15, then Monthly):
                // evaluate the partial remainder of the current month only,
                // then resume full calendar months afterwards.
                monthStart = nextStart;
                monthEnd = nextMonthStart.AddMonths(1).AddDays(-1);
            }

            if (monthEnd >= todayCairo)
            {
                return null;
            }

            return (monthStart, monthEnd);
        }

        var length = WindowLengthDays(period);

        var start = lastCompletedEndCairo is null
            ? todayCairo.AddDays(-length)
            : lastCompletedEndCairo.Value.AddDays(1);

        var end = start.AddDays(length - 1);

        if (end >= todayCairo)
        {
            return null;
        }

        return (start, end);
    }

    public static PeriodWindow ToUtcWindow(DateOnly startCairo, DateOnly endCairo, TimeZoneInfo cairoTimeZone)
    {
        var startUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(startCairo.ToDateTime(TimeOnly.MinValue), cairoTimeZone));

        var endExclusiveLocal = endCairo.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var endUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(endExclusiveLocal, cairoTimeZone)).AddTicks(-1);

        return new PeriodWindow(startUtc, endUtc);
    }
}
