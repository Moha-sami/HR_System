using Buy2.Domain.Entities;

namespace Buy2.Application.Common.Helpers;

/// <summary>
/// Shared normalization for leave <see cref="Request"/> statuses.
/// Single source of truth so the attendance calendar and template application
/// can never drift on what counts as approved leave vs. remote work.
/// </summary>
public static class LeaveStatusHelper
{
    public static bool IsApprovedLeaveStatus(string? status)
    {
        return IsLeaveStatus(status) || IsRemoteWorkStatus(status);
    }

    public static bool IsRemoteWorkStatus(string? status)
    {
        return Normalize(status) == "remote work";
    }

    public static bool CoversDate(Request request, DateOnly date)
    {
        if (!request.StartDate.HasValue)
        {
            return false;
        }

        var start = DateOnly.FromDateTime(request.StartDate.Value.Date);
        var end = request.EndDate.HasValue
            ? DateOnly.FromDateTime(request.EndDate.Value.Date)
            : start;

        return date >= start && date <= end;
    }

    public static string FormatLeaveType(Request? request)
    {
        if (request?.RequestType != null && !string.IsNullOrWhiteSpace(request.RequestType.Name))
        {
            return request.RequestType.Name;
        }

        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            return request.Reason;
        }

        return "Approved Leave";
    }

    private static bool IsLeaveStatus(string? status)
    {
        return Normalize(status) switch
        {
            "approved" => true,
            "approved leave" => true,
            "approvedleave" => true,
            "sick leave" => true,
            "remote work" => true,
            _ => false,
        };
    }

    private static string Normalize(string? status)
    {
        return status?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
