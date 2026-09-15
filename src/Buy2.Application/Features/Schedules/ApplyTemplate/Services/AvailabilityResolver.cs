using Buy2.Application.Common.Helpers;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class AvailabilityResolver : IAvailabilityResolver
{
    private readonly IRepository<EmployeeSite> _employeeSiteRepository;
    private readonly IRepository<SiteOperationalHour> _operationalHourRepository;
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;

    public AvailabilityResolver(
        IRepository<EmployeeSite> employeeSiteRepository,
        IRepository<SiteOperationalHour> operationalHourRepository,
        IRepository<Request> requestRepository,
        IRepository<AttendanceRecord> attendanceRepository)
    {
        _employeeSiteRepository = employeeSiteRepository;
        _operationalHourRepository = operationalHourRepository;
        _requestRepository = requestRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<AvailabilityContext> ResolveAsync(
        int siteId,
        Site site,
        DateOnly date,
        IEnumerable<int> candidateEmployeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = candidateEmployeeIds.ToList();
        var hours = await ResolveOperationalHoursAsync(site, cancellationToken);
        hours.TryGetValue(date.DayOfWeek, out var dayHours);

        var context = new AvailabilityContext
        {
            AuthorizedEmployeeIds = await LoadAuthorizedIdsAsync(siteId, ids, cancellationToken),
            IsDayOff = dayHours != null && !dayHours.IsOpen,
            DayHours = dayHours,
        };

        await LoadLeaveAsync(context, ids, date, cancellationToken);
        return context;
    }

    private async Task<HashSet<int>> LoadAuthorizedIdsAsync(
        int siteId, List<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var specification = new Specification<EmployeeSite>()
            .Where(l => l.SiteId == siteId && ids.Contains(l.EmployeeId));

        var authorized = await _employeeSiteRepository.ListAsync(
            specification, l => l.EmployeeId, cancellationToken);

        return authorized.ToHashSet();
    }

    private async Task LoadLeaveAsync(
        AvailabilityContext context, List<int> ids, DateOnly date, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var requests = await _requestRepository.ListAsync(
            r => ids.Contains(r.EmployeeId)
                && r.StartDate.HasValue && r.StartDate.Value < dayEnd
                && (!r.EndDate.HasValue || r.EndDate.Value >= dayStart),
            cancellationToken,
            nameof(Request.RequestType));

        foreach (var req in requests)
        {
            AddCoveringLeave(context, req, date);
        }

        var records = await _attendanceRepository.ListAsync(
            a => ids.Contains(a.EmployeeId) && a.Date >= dayStart && a.Date < dayEnd,
            cancellationToken);

        AddLeaveRecords(context, records);
    }

    private static void AddCoveringLeave(AvailabilityContext context, Request req, DateOnly date)
    {
        if (!LeaveStatusHelper.IsApprovedLeaveStatus(req.Status)
            || !LeaveStatusHelper.CoversDate(req, date))
        {
            return;
        }

        context.LeaveRequests.Add(req);
        context.LeaveTypeByEmployeeId[req.EmployeeId] = LeaveStatusHelper.FormatLeaveType(req);
        context.RemoteWorkByEmployeeId[req.EmployeeId] = LeaveStatusHelper.IsRemoteWorkStatus(req.Status);
    }

    private static void AddLeaveRecords(AvailabilityContext context, List<AttendanceRecord> records)
    {
        foreach (var record in records)
        {
            if (IsLeaveRecord(record))
            {
                context.LeaveRecordEmployeeIds.Add(record.EmployeeId);
            }
        }
    }

    private static bool IsLeaveRecord(AttendanceRecord record)
    {
        return record.Status is AttendanceDayStatus.ApprovedLeave
            or AttendanceDayStatus.UnapprovedLeave
            or AttendanceDayStatus.PartialLeave;
    }

    private async Task<Dictionary<DayOfWeek, SiteOperationalHour>> ResolveOperationalHoursAsync(
        Site site, CancellationToken cancellationToken)
    {
        var fromRepo = await _operationalHourRepository.ListAsync(
            o => o.SiteId == site.Id, cancellationToken);

        if (fromRepo.Count > 0)
        {
            return fromRepo.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
        }

        var source = site.OperationalHours ?? Enumerable.Empty<SiteOperationalHour>();
        return source.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
    }
}
