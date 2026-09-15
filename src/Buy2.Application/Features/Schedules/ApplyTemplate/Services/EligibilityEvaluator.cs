using Buy2.Application.Common.Helpers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class EligibilityEvaluator : IEligibilityEvaluator
{
    public List<PlannedBlock> BuildPlannedBlocks(
        int siteId,
        DateOnly date,
        int templateId,
        List<ShiftBlock> blocks,
        Dictionary<int, Employee> employees,
        Dictionary<int, string> roleTitles,
        AvailabilityContext availability)
    {
        var planned = new List<PlannedBlock>(blocks.Count);

        foreach (var block in blocks)
        {
            var roleTitle = ResolveRoleTitle(block.JobRoleId, roleTitles);
            employees.TryGetValue(block.EmployeeId ?? -1, out var employee);
            var strip = ResolveStrip(block, employee, roleTitle, date, availability);

            var start = ToDateTimeOffset(date, block.StartTime);
            var end = ToDateTimeOffset(date, block.EndTime);
            if (end <= start)
            {
                end = end.AddDays(1);
            }

            var entity = new ShiftEntity
            {
                SiteId = siteId,
                JobRoleId = block.JobRoleId,
                StartTime = start,
                EndTime = end,
                IsPublished = false,
                Status = ShiftStatus.Draft,
                EmployeeId = strip != null ? null : employee!.Id,
                ShiftTemplateId = templateId,
            };

            planned.Add(new PlannedBlock
            {
                Entity = entity,
                RoleTitle = roleTitle,
                SourceEmployeeId = block.EmployeeId,
                SourceEmployeeName = employee == null ? null : DisplayName(employee),
                AssignedEmployeeName = strip != null || employee == null ? null : DisplayName(employee),
                Stripped = strip != null,
                StripCode = strip?.Code,
                StripReason = strip?.Reason,
            });
        }

        return planned;
    }

    private static string ResolveRoleTitle(int jobRoleId, Dictionary<int, string> roleTitles)
    {
        return roleTitles.TryGetValue(jobRoleId, out var title) ? title : $"Role {jobRoleId}";
    }

    private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeSpan time)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(time)), TimeSpan.Zero);
    }

    private static StripDecision? ResolveStrip(
        ShiftBlock block,
        Employee? employee,
        string roleTitle,
        DateOnly date,
        AvailabilityContext availability)
    {
        return ResolveStripIdentity(block, employee, roleTitle, availability)
            ?? ResolveStripAvailability(block, employee!, roleTitle, date, availability);
    }

    private static StripDecision? ResolveStripIdentity(
        ShiftBlock block,
        Employee? employee,
        string roleTitle,
        AvailabilityContext availability)
    {
        if (block.EmployeeId == null || employee == null)
        {
            return new StripDecision(
                ApplyTemplateStripCodes.EmployeeNotFound,
                "Employee assigned to template block is not found.");
        }

        if (employee.IsDeleted || !employee.IsActive)
        {
            return new StripDecision(
                ApplyTemplateStripCodes.EmployeeInactive,
                $"{DisplayName(employee)} is inactive and cannot be scheduled.");
        }

        if (!availability.AuthorizedEmployeeIds.Contains(employee.Id))
        {
            return new StripDecision(
                ApplyTemplateStripCodes.SiteUnauthorized,
                "Employee is unavailable at target site.");
        }

        return null;
    }

    private static StripDecision? ResolveStripAvailability(
        ShiftBlock block,
        Employee employee,
        string roleTitle,
        DateOnly date,
        AvailabilityContext availability)
    {
        if (availability.IsDayOff)
        {
            return new StripDecision(
                ApplyTemplateStripCodes.SiteClosed,
                $"Site is closed on {date.DayOfWeek}.");
        }

        var hoursStrip = StripForSiteHours(block, availability.DayHours);
        if (hoursStrip != null)
        {
            return hoursStrip;
        }

        var leaveStrip = StripForLeave(employee, date, availability);
        if (leaveStrip != null)
        {
            return leaveStrip;
        }

        if (WeeklyAvailabilityHelper.Evaluate(
                employee.OnlineWorkdaysJson, employee.OfflineWorkdaysJson, date.DayOfWeek)
            == WeeklyAvailability.Unavailable)
        {
            return new StripDecision(
                ApplyTemplateStripCodes.OutsideEmployeeAvailability,
                $"{DisplayName(employee)} is unavailable on {date.DayOfWeek} per weekly availability.");
        }

        if (employee.JobRoleId != block.JobRoleId)
        {
            return new StripDecision(
                ApplyTemplateStripCodes.QualificationMismatch,
                $"{DisplayName(employee)} job role does not match required role {roleTitle}.");
        }

        return null;
    }

    private static StripDecision? StripForSiteHours(ShiftBlock block, SiteOperationalHour? dayHours)
    {
        if (dayHours == null)
        {
            return null;
        }

        var open = dayHours.OpenTime.ToTimeSpan();
        var close = dayHours.CloseTime.ToTimeSpan();

        if (block.StartTime < open || block.EndTime > close)
        {
            return new StripDecision(
                ApplyTemplateStripCodes.OutsideOperationalHours,
                "Block time falls outside site operational hours.");
        }

        return null;
    }

    private static StripDecision? StripForLeave(
        Employee employee, DateOnly date, AvailabilityContext availability)
    {
        if (availability.LeaveTypeByEmployeeId.TryGetValue(employee.Id, out var leaveType))
        {
            var isRemote = availability.RemoteWorkByEmployeeId.TryGetValue(employee.Id, out var remote) && remote;
            var code = isRemote
                ? ApplyTemplateStripCodes.EmployeeRemoteWork
                : ApplyTemplateStripCodes.EmployeeOnLeave;
            var kind = isRemote ? "remote work" : "approved leave";
            return new StripDecision(code, $"On {kind} ({leaveType}) on {date:yyyy-MM-dd}.");
        }

        if (availability.LeaveRecordEmployeeIds.Contains(employee.Id))
        {
            return new StripDecision(
                ApplyTemplateStripCodes.EmployeeOnLeave,
                $"On approved leave on {date:yyyy-MM-dd}.");
        }

        return null;
    }

    private static string DisplayName(Employee employee)
    {
        return $"{employee.FirstName} {employee.LastName}".Trim();
    }
}
