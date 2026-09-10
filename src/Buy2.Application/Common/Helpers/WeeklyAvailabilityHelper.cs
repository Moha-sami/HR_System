using System.Text.Json;

namespace Buy2.Application.Common.Helpers;

/// <summary>
/// Tri-state result of the weekly workdays availability check.
/// Unknown means there is no positive evidence either way and must never strip.
/// </summary>
public enum WeeklyAvailability
{
    Available,
    Unavailable,
    Unknown
}

/// <summary>
/// Evaluates <c>OnlineWorkdaysJson / OfflineWorkdaysJson</c> (JSON string arrays
/// of full day names, e.g. ["Sunday","Monday"]) for a target day of week.
/// Offline wins on conflict; empty/missing/malformed data yields Unknown.
/// </summary>
public static class WeeklyAvailabilityHelper
{
    public static WeeklyAvailability Evaluate(
        string? onlineWorkdaysJson,
        string? offlineWorkdaysJson,
        DayOfWeek dayOfWeek)
    {
        var dayName = dayOfWeek.ToString();
        var offline = Parse(offlineWorkdaysJson);
        var online = Parse(onlineWorkdaysJson);

        if (Contains(offline, dayName))
        {
            return WeeklyAvailability.Unavailable;
        }

        if (Contains(online, dayName))
        {
            return WeeklyAvailability.Available;
        }

        return WeeklyAvailability.Unknown;
    }

    private static bool Contains(HashSet<string> days, string dayName)
    {
        return days.Contains(dayName);
    }

    private static HashSet<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return new HashSet<string>(list ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
