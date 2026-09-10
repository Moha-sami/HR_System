using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.CommitPublishSchedule;

public class CommitPublishScheduleCommandHandler : IRequestHandler<CommitPublishScheduleCommand, CommitPublishScheduleResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CommitPublishScheduleCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<Request> requestRepository,
        IRepository<Notification> notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _requestRepository = requestRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommitPublishScheduleResponseDto> Handle(
        CommitPublishScheduleCommand request,
        CancellationToken cancellationToken)
    {
        await ValidateSiteExistsAsync(request.SiteId, cancellationToken);
        ValidateInputTargets(request);

        var shifts = await LoadUnpublishedShiftsAsync(request, cancellationToken);
        if (shifts.Count == 0)
        {
            return BuildEmptyResponse(request.SiteId);
        }

        var employees = await LoadAssignedEmployeesAsync(shifts, cancellationToken);
        var shiftExceptions = await EvaluateExceptionsAsync(shifts, employees, cancellationToken);
        ValidateOvertimeJustification(shifts, shiftExceptions, request);

        return await ExecuteCommitAsync(request, shifts, shiftExceptions, cancellationToken);
    }

    private async Task ValidateSiteExistsAsync(int siteId, CancellationToken cancellationToken)
    {
        var exists = await _siteRepository.AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Site with ID {siteId} not found.");
        }
    }

    private static void ValidateInputTargets(CommitPublishScheduleCommand request)
    {
        var hasDates = request.TargetDates != null && request.TargetDates.Count > 0;
        if (!hasDates && !request.AllUnpublishedDays)
        {
            throw new ArgumentException("Either TargetDates or AllUnpublishedDays must be specified.");
        }
    }

    private async Task<List<ShiftEntity>> LoadUnpublishedShiftsAsync(
        CommitPublishScheduleCommand request,
        CancellationToken cancellationToken)
    {
        var query = _shiftRepository.Query(false)
            .Include(s => s.JobRole)
            .Where(s => !s.IsPublished && s.SiteId == request.SiteId);

        query = ApplyRoleFilter(query, request.TargetRoleIds);

        var shifts = await query.ToListAsync(cancellationToken);
        return FilterByTargetDates(shifts, request);
    }

    private static IQueryable<ShiftEntity> ApplyRoleFilter(
        IQueryable<ShiftEntity> query,
        IReadOnlyList<int>? roleIds)
    {
        if (roleIds != null && roleIds.Count > 0)
        {
            return query.Where(s => roleIds.Contains(s.JobRoleId));
        }

        return query;
    }

    private static List<ShiftEntity> FilterByTargetDates(
        List<ShiftEntity> shifts,
        CommitPublishScheduleCommand request)
    {
        if (request.AllUnpublishedDays || request.TargetDates == null || request.TargetDates.Count == 0)
        {
            return shifts;
        }

        var dateSet = request.TargetDates.ToHashSet();
        return shifts.Where(s => IsShiftInDates(s, dateSet)).ToList();
    }

    private static bool IsShiftInDates(ShiftEntity shift, HashSet<DateOnly> dates)
    {
        return dates.Contains(DateOnly.FromDateTime(shift.StartTime.Date)) ||
               dates.Contains(DateOnly.FromDateTime(shift.StartTime.UtcDateTime.Date));
    }

    private async Task<Dictionary<int, Employee>> LoadAssignedEmployeesAsync(
        IReadOnlyList<ShiftEntity> shifts,
        CancellationToken cancellationToken)
    {
        var employeeIds = shifts
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
        {
            return new Dictionary<int, Employee>();
        }

        return await _employeeRepository.Query(true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private async Task<Dictionary<int, ShiftExceptionInfo>> EvaluateExceptionsAsync(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees,
        CancellationToken cancellationToken)
    {
        var exceptions = new Dictionary<int, ShiftExceptionInfo>();
        var weeklyCache = new Dictionary<(int, DateTimeOffset), List<ShiftEntity>>();

        foreach (var shift in shifts.Where(s => s.EmployeeId.HasValue))
        {
            if (!employees.TryGetValue(shift.EmployeeId!.Value, out var emp))
            {
                continue;
            }

            var isUnqualified = emp.JobRoleId != shift.JobRoleId;
            var hasOvertime = await CheckOvertimeViolationAsync(shift, emp, weeklyCache, cancellationToken);
            exceptions[shift.Id] = new ShiftExceptionInfo(isUnqualified, hasOvertime);
        }

        return exceptions;
    }

    private async Task<bool> CheckOvertimeViolationAsync(
        ShiftEntity shift,
        Employee emp,
        Dictionary<(int, DateTimeOffset), List<ShiftEntity>> cache,
        CancellationToken cancellationToken)
    {
        var (weekStart, weekEnd) = GetWeekBoundary(GetShiftDate(shift));
        var weeklyShifts = await GetEmployeeWeeklyShiftsAsync(emp.Id, weekStart, weekEnd, cache, cancellationToken);
        var shiftHours = (decimal)(shift.EndTime - shift.StartTime).TotalHours;
        var totalWeekly = CalculateTotalWeeklyHours(weeklyShifts, shift, shiftHours);
        var threshold = ResolveOvertimeThreshold(emp.PayrollProfile);

        return shiftHours > 8.0m || totalWeekly > threshold;
    }

    private static decimal CalculateTotalWeeklyHours(
        IReadOnlyList<ShiftEntity> weeklyShifts,
        ShiftEntity currentShift,
        decimal currentShiftHours)
    {
        var total = weeklyShifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours);
        if (!weeklyShifts.Any(s => s.Id == currentShift.Id))
        {
            total += currentShiftHours;
        }

        return total;
    }

    private static decimal ResolveOvertimeThreshold(PayrollProfile? profile)
    {
        if (profile != null && profile.OvertimeThresholdHours > 0)
        {
            return profile.OvertimeThresholdHours;
        }

        return 40.0m;
    }

    private async Task<List<ShiftEntity>> GetEmployeeWeeklyShiftsAsync(
        int employeeId,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        Dictionary<(int, DateTimeOffset), List<ShiftEntity>> cache,
        CancellationToken cancellationToken)
    {
        var key = (employeeId, weekStart);
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var shifts = await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId == employeeId && s.StartTime >= weekStart && s.StartTime < weekEnd)
            .ToListAsync(cancellationToken);

        cache[key] = shifts;
        return shifts;
    }

    private static (DateTimeOffset WeekStart, DateTimeOffset WeekEnd) GetWeekBoundary(DateOnly targetDate)
    {
        int diff = (7 + (int)targetDate.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var monday = targetDate.AddDays(-diff);
        var sunday = monday.AddDays(6);

        var weekStart = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var weekEnd = new DateTimeOffset(sunday.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return (weekStart, weekEnd);
    }

    private static DateOnly GetShiftDate(ShiftEntity shift)
    {
        return DateOnly.FromDateTime(shift.StartTime.Date);
    }

    private static void ValidateOvertimeJustification(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, ShiftExceptionInfo> shiftExceptions,
        CommitPublishScheduleCommand request)
    {
        var hasPublishingOvertime = shifts.Any(s => IsOvertimeBeingPublished(s.Id, shiftExceptions, request.ExceptionResolutions));
        if (hasPublishingOvertime && string.IsNullOrWhiteSpace(request.OvertimeJustification))
        {
            throw new ArgumentException("Overtime justification explanation is mandatory when publishing overtime shifts.");
        }
    }

    private static bool IsOvertimeBeingPublished(
        int shiftId,
        IReadOnlyDictionary<int, ShiftExceptionInfo> exceptions,
        IReadOnlyDictionary<int, PublishExceptionDecision>? resolutions)
    {
        if (!exceptions.TryGetValue(shiftId, out var info))
        {
            return false;
        }

        return info.HasOvertime && !IsShiftSkipped(shiftId, resolutions);
    }

    private async Task<CommitPublishScheduleResponseDto> ExecuteCommitAsync(
        CommitPublishScheduleCommand request,
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, ShiftExceptionInfo> shiftExceptions,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var summary = await ProcessShiftsAsync(request, shifts, shiftExceptions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return BuildSuccessResponse(request.SiteId, summary);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<ShiftBatchSummary> ProcessShiftsAsync(
        CommitPublishScheduleCommand request,
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, ShiftExceptionInfo> shiftExceptions,
        CancellationToken cancellationToken)
    {
        var summary = new ShiftBatchSummary();
        foreach (var shift in shifts)
        {
            await ProcessSingleShiftAsync(request, shift, shiftExceptions, summary, cancellationToken);
        }

        return summary;
    }

    private async Task ProcessSingleShiftAsync(
        CommitPublishScheduleCommand request,
        ShiftEntity shift,
        IReadOnlyDictionary<int, ShiftExceptionInfo> exceptions,
        ShiftBatchSummary summary,
        CancellationToken cancellationToken)
    {
        if (IsShiftSkipped(shift.Id, request.ExceptionResolutions))
        {
            ApplySkippedShift(shift, summary);
            return;
        }

        exceptions.TryGetValue(shift.Id, out var info);
        if (info.HasOvertime)
        {
            await ApplyOvertimeShiftAsync(shift, request.OvertimeJustification ?? string.Empty, info.IsUnqualified, summary, cancellationToken);
            return;
        }

        await ApplyCompliantOrUnqualifiedShiftAsync(shift, info.IsUnqualified, summary, cancellationToken);
    }

    private static bool IsShiftSkipped(
        int shiftId,
        IReadOnlyDictionary<int, PublishExceptionDecision>? resolutions)
    {
        if (resolutions != null && resolutions.TryGetValue(shiftId, out var decision))
        {
            return decision == PublishExceptionDecision.Skip;
        }

        return false;
    }

    private static void ApplySkippedShift(ShiftEntity shift, ShiftBatchSummary summary)
    {
        shift.IsPublished = false;
        shift.Status = ShiftStatus.Draft;
        summary.SkippedIds.Add(shift.Id);
    }

    private async Task ApplyOvertimeShiftAsync(
        ShiftEntity shift,
        string justification,
        bool isUnqualified,
        ShiftBatchSummary summary,
        CancellationToken cancellationToken)
    {
        shift.IsPublished = false;
        shift.Status = ShiftStatus.PendingHrApproval;
        if (isUnqualified)
        {
            shift.HasUnqualifiedOverride = true;
        }

        var hrRequest = CreateHrApprovalRequest(shift, justification);
        await _requestRepository.AddAsync(hrRequest, cancellationToken);
        summary.PendingHrApprovalIds.Add(shift.Id);
    }

    private async Task ApplyCompliantOrUnqualifiedShiftAsync(
        ShiftEntity shift,
        bool isUnqualified,
        ShiftBatchSummary summary,
        CancellationToken cancellationToken)
    {
        shift.IsPublished = true;
        shift.Status = ShiftStatus.Published;
        if (isUnqualified)
        {
            shift.HasUnqualifiedOverride = true;
        }

        if (shift.EmployeeId.HasValue)
        {
            var notification = CreatePublicationNotification(shift, isUnqualified);
            await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        summary.PublishedIds.Add(shift.Id);
    }

    private static Request CreateHrApprovalRequest(ShiftEntity shift, string justification)
    {
        return new Request
        {
            EmployeeId = shift.EmployeeId!.Value,
            RequestTypeId = 1,
            StartDate = shift.StartTime.UtcDateTime,
            EndDate = shift.EndTime.UtcDateTime,
            Reason = justification,
            Status = "Pending HR Approval",
            SubmittedAt = DateTime.UtcNow
        };
    }

    private static Notification CreatePublicationNotification(ShiftEntity shift, bool isUnqualified)
    {
        var title = isUnqualified ? "Shift Published with Override" : "Shift Published";
        var dateStr = DateOnly.FromDateTime(shift.StartTime.Date).ToString("yyyy-MM-dd");

        return new Notification
        {
            EmployeeId = shift.EmployeeId!.Value,
            Title = title,
            Body = $"Your shift on {dateStr} has been published.",
            Type = "ShiftPublication",
            ReferenceId = shift.Id.ToString(),
            ReferenceType = "Shift",
            IsRead = false
        };
    }

    private static CommitPublishScheduleResponseDto BuildSuccessResponse(
        int siteId,
        ShiftBatchSummary summary)
    {
        var total = summary.PublishedIds.Count + summary.PendingHrApprovalIds.Count + summary.SkippedIds.Count;
        var message = $"Successfully processed {total} shifts ({summary.PublishedIds.Count} published immediately, {summary.PendingHrApprovalIds.Count} pending HR approval, {summary.SkippedIds.Count} skipped).";

        return new CommitPublishScheduleResponseDto(
            Success: true,
            SiteId: siteId,
            TotalShiftsProcessed: total,
            PublishedImmediatelyCount: summary.PublishedIds.Count,
            PendingHrApprovalCount: summary.PendingHrApprovalIds.Count,
            SkippedCount: summary.SkippedIds.Count,
            PublishedShiftIds: summary.PublishedIds,
            PendingHrApprovalShiftIds: summary.PendingHrApprovalIds,
            SkippedShiftIds: summary.SkippedIds,
            Message: message
        );
    }

    private static CommitPublishScheduleResponseDto BuildEmptyResponse(int siteId)
    {
        return new CommitPublishScheduleResponseDto(
            Success: true,
            SiteId: siteId,
            TotalShiftsProcessed: 0,
            PublishedImmediatelyCount: 0,
            PendingHrApprovalCount: 0,
            SkippedCount: 0,
            PublishedShiftIds: new List<int>(),
            PendingHrApprovalShiftIds: new List<int>(),
            SkippedShiftIds: new List<int>(),
            Message: "No unpublished shifts found matching the specified criteria."
        );
    }

    private readonly record struct ShiftExceptionInfo(bool IsUnqualified, bool HasOvertime);

    private sealed class ShiftBatchSummary
    {
        public List<int> PublishedIds { get; } = new();
        public List<int> PendingHrApprovalIds { get; } = new();
        public List<int> SkippedIds { get; } = new();
    }
}
