using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.AutoFillDailyShifts;

public class AutoFillDailyShiftsCommandHandler : IRequestHandler<AutoFillDailyShiftsCommand, AutoFillDailyShiftsResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SitePreferredEmployee> _sitePreferredRepository;
    private readonly IRepository<SiteOperationalHour>? _operationalHourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AutoFillDailyShiftsCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<SitePreferredEmployee> sitePreferredRepository,
        IUnitOfWork unitOfWork)
        : this(siteRepository, shiftRepository, employeeRepository, sitePreferredRepository, unitOfWork, null)
    {
    }

    public AutoFillDailyShiftsCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<SitePreferredEmployee> sitePreferredRepository,
        IUnitOfWork unitOfWork,
        IRepository<SiteOperationalHour>? operationalHourRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _sitePreferredRepository = sitePreferredRepository;
        _unitOfWork = unitOfWork;
        _operationalHourRepository = operationalHourRepository;
    }

    public async Task<AutoFillDailyShiftsResponseDto> Handle(AutoFillDailyShiftsCommand request, CancellationToken cancellationToken)
    {
        var site = await ValidateSiteAsync(request.SiteId, cancellationToken);
        var operationalHours = await ResolveOperationalHoursAsync(site, cancellationToken);
        var targetDateShifts = await GetTargetDateShiftsAsync(request.SiteId, request.Date, cancellationToken);

        var openBlocks = targetDateShifts.Where(s => s.EmployeeId == null).OrderBy(s => s.StartTime).ToList();
        var candidates = await GetCandidateEmployeesAsync(cancellationToken);
        var preferredIds = await GetPreferredEmployeeIdsAsync(request.SiteId, cancellationToken);

        var (weekStart, weekEnd) = GetWeekBoundary(request.Date);
        var candidateIds = candidates.Select(c => c.Id).ToHashSet();
        var weeklyShifts = await GetWeeklyShiftsAsync(candidateIds, weekStart, weekEnd, cancellationToken);

        var weeklyHours = InitWeeklyHours(candidates, weeklyShifts);
        var dayShifts = InitDayShifts(candidates, weeklyShifts, request.Date);

        var (assignedDetails, unfillableCount) = ExecuteMatching(openBlocks, candidates, preferredIds, weeklyHours, dayShifts);

        if (assignedDetails.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var isDayOff = IsSiteDayOff(request.Date.DayOfWeek, operationalHours);
        var allEmployees = await LoadAllAssignedEmployeesAsync(targetDateShifts, candidates, cancellationToken);
        var updatedLaborCost = CalculateLaborCost(targetDateShifts, allEmployees);
        var coverageStatus = DetermineCoverageStatus(isDayOff, targetDateShifts, allEmployees);
        var summaryMessage = BuildSummaryMessage(openBlocks.Count, assignedDetails.Count, unfillableCount);

        return new AutoFillDailyShiftsResponseDto(
            SiteId: request.SiteId,
            Date: request.Date,
            TotalOpenBlocksScanned: openBlocks.Count,
            SuccessfullyAssignedCount: assignedDetails.Count,
            UnfillableCount: unfillableCount,
            AssignedBlocks: assignedDetails,
            UpdatedDailyLaborCost: updatedLaborCost,
            CoverageStatus: coverageStatus,
            SummaryMessage: summaryMessage
        );
    }

    private async Task<Site> ValidateSiteAsync(int siteId, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.Query(true)
            .Include(s => s.OperationalHours)
            .FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);

        if (site == null)
        {
            throw new KeyNotFoundException($"Site with ID {siteId} not found.");
        }

        return site;
    }

    private async Task<Dictionary<DayOfWeek, SiteOperationalHour>> ResolveOperationalHoursAsync(Site site, CancellationToken cancellationToken)
    {
        if (_operationalHourRepository != null)
        {
            var fromRepo = await _operationalHourRepository.Query(true)
                .Where(o => o.SiteId == site.Id)
                .ToListAsync(cancellationToken);

            if (fromRepo.Count > 0)
            {
                return fromRepo.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
            }
        }

        var source = site.OperationalHours ?? Enumerable.Empty<SiteOperationalHour>();
        return source.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
    }

    private async Task<List<ShiftEntity>> GetTargetDateShiftsAsync(int siteId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var shifts = await _shiftRepository.Query(false)
            .Include(s => s.JobRole)
            .Where(s => s.SiteId == siteId && s.StartTime >= dayStart.AddDays(-1) && s.StartTime < dayEnd.AddDays(1))
            .ToListAsync(cancellationToken);

        return shifts.Where(s => IsShiftOnDate(s, date)).OrderBy(s => s.StartTime).ToList();
    }

    private static bool IsShiftOnDate(ShiftEntity shift, DateOnly date)
    {
        return DateOnly.FromDateTime(shift.StartTime.Date) == date ||
               DateOnly.FromDateTime(shift.StartTime.UtcDateTime.Date) == date;
    }

    private async Task<List<Employee>> GetCandidateEmployeesAsync(CancellationToken cancellationToken)
    {
        return await _employeeRepository.Query(true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .Where(e => !e.IsDeleted && e.IsActive)
            .ToListAsync(cancellationToken);
    }

    private async Task<HashSet<int>> GetPreferredEmployeeIdsAsync(int siteId, CancellationToken cancellationToken)
    {
        var preferred = await _sitePreferredRepository.Query(true)
            .Where(p => p.SiteId == siteId)
            .ToListAsync(cancellationToken);

        return preferred.Select(p => p.EmployeeId).ToHashSet();
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

    private async Task<List<ShiftEntity>> GetWeeklyShiftsAsync(
        ISet<int> candidateIds,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        CancellationToken cancellationToken)
    {
        if (candidateIds.Count == 0)
        {
            return new List<ShiftEntity>();
        }

        return await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId != null && candidateIds.Contains(s.EmployeeId.Value) && s.StartTime >= weekStart && s.StartTime < weekEnd)
            .ToListAsync(cancellationToken);
    }

    private static Dictionary<int, decimal> InitWeeklyHours(IEnumerable<Employee> candidates, IReadOnlyList<ShiftEntity> weeklyShifts)
    {
        return candidates.ToDictionary(
            e => e.Id,
            e => weeklyShifts.Where(s => s.EmployeeId == e.Id).Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours)
        );
    }

    private static Dictionary<int, List<ShiftEntity>> InitDayShifts(
        IEnumerable<Employee> candidates,
        IReadOnlyList<ShiftEntity> weeklyShifts,
        DateOnly targetDate)
    {
        return candidates.ToDictionary(
            e => e.Id,
            e => weeklyShifts.Where(s => s.EmployeeId == e.Id && IsShiftOnDate(s, targetDate)).ToList()
        );
    }

    private static (List<AutoFillAssignmentDetailDto> Assigned, int UnfillableCount) ExecuteMatching(
        IReadOnlyList<ShiftEntity> openBlocks,
        IReadOnlyList<Employee> candidates,
        ISet<int> preferredIds,
        IDictionary<int, decimal> weeklyHours,
        IDictionary<int, List<ShiftEntity>> dayShifts)
    {
        var assignedDetails = new List<AutoFillAssignmentDetailDto>();
        int unfillable = 0;

        foreach (var block in openBlocks)
        {
            var duration = (decimal)Math.Max(0, (block.EndTime - block.StartTime).TotalHours);
            var chosen = FindBestCandidate(block, duration, candidates, preferredIds, weeklyHours, dayShifts);

            if (chosen != null)
            {
                AssignBlock(block, chosen, duration, preferredIds.Contains(chosen.Id), weeklyHours, dayShifts, assignedDetails);
            }
            else
            {
                unfillable++;
            }
        }

        return (assignedDetails, unfillable);
    }

    private static Employee? FindBestCandidate(
        ShiftEntity block,
        decimal blockDuration,
        IReadOnlyList<Employee> candidates,
        ISet<int> preferredIds,
        IDictionary<int, decimal> weeklyHours,
        IDictionary<int, List<ShiftEntity>> dayShifts)
    {
        return candidates
            .Where(e => IsCandidateEligible(e, block, blockDuration, weeklyHours, dayShifts))
            .OrderByDescending(e => preferredIds.Contains(e.Id))
            .ThenBy(e => weeklyHours[e.Id])
            .ThenByDescending(e => e.Id)
            .FirstOrDefault();
    }

    private static bool IsCandidateEligible(
        Employee emp,
        ShiftEntity block,
        decimal blockDuration,
        IDictionary<int, decimal> weeklyHours,
        IDictionary<int, List<ShiftEntity>> dayShifts)
    {
        if (emp.JobRoleId != block.JobRoleId)
        {
            return false;
        }

        if (HasOverlappingShift(block, dayShifts[emp.Id]))
        {
            return false;
        }

        if (ExceedsHoursLimit(emp, blockDuration, weeklyHours[emp.Id], dayShifts[emp.Id]))
        {
            return false;
        }

        return true;
    }

    private static bool HasOverlappingShift(ShiftEntity block, IReadOnlyList<ShiftEntity> existingShifts)
    {
        return existingShifts.Any(s => block.StartTime < s.EndTime && s.StartTime < block.EndTime);
    }

    private static bool ExceedsHoursLimit(
        Employee emp,
        decimal blockDuration,
        decimal currentWeeklyHours,
        IReadOnlyList<ShiftEntity> currentDayShifts)
    {
        if (blockDuration > 8.0m)
        {
            return true;
        }

        var dailyHours = currentDayShifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours);
        if (dailyHours + blockDuration > 8.0m)
        {
            return true;
        }

        var weeklyThreshold = emp.PayrollProfile?.OvertimeThresholdHours > 0
            ? emp.PayrollProfile.OvertimeThresholdHours
            : 40m;

        return currentWeeklyHours + blockDuration > weeklyThreshold;
    }

    private static void AssignBlock(
        ShiftEntity block,
        Employee chosen,
        decimal duration,
        bool isPreferred,
        IDictionary<int, decimal> weeklyHours,
        IDictionary<int, List<ShiftEntity>> dayShifts,
        List<AutoFillAssignmentDetailDto> assignedDetails)
    {
        block.EmployeeId = chosen.Id;
        weeklyHours[chosen.Id] += duration;
        dayShifts[chosen.Id].Add(block);

        assignedDetails.Add(new AutoFillAssignmentDetailDto(
            ShiftId: block.Id,
            EmployeeId: chosen.Id,
            EmployeeName: $"{chosen.FirstName} {chosen.LastName}".Trim(),
            RoleTitle: block.JobRole?.Title ?? chosen.JobRole?.Title ?? string.Empty,
            StartTime: block.StartTime,
            EndTime: block.EndTime,
            IsPreferredEmployee: isPreferred
        ));
    }

    private static bool IsSiteDayOff(DayOfWeek dayOfWeek, IReadOnlyDictionary<DayOfWeek, SiteOperationalHour> operationalHours)
    {
        if (operationalHours.TryGetValue(dayOfWeek, out var hourRecord))
        {
            return !hourRecord.IsOpen;
        }

        return false;
    }

    private async Task<Dictionary<int, Employee>> LoadAllAssignedEmployeesAsync(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyList<Employee> knownCandidates,
        CancellationToken cancellationToken)
    {
        var dict = knownCandidates.ToDictionary(e => e.Id);
        var missingIds = shifts
            .Where(s => s.EmployeeId.HasValue && !dict.ContainsKey(s.EmployeeId.Value))
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (missingIds.Count > 0)
        {
            var missing = await _employeeRepository.Query(true)
                .Include(e => e.PayrollProfile)
                .Include(e => e.JobRole)
                .Where(e => missingIds.Contains(e.Id))
                .ToListAsync(cancellationToken);

            foreach (var emp in missing)
            {
                dict[emp.Id] = emp;
            }
        }

        return dict;
    }

    private static decimal CalculateHourlyRate(PayrollProfile? payroll)
    {
        if (payroll == null)
        {
            return 0m;
        }

        if (string.Equals(payroll.SalaryType, "Monthly", StringComparison.OrdinalIgnoreCase) && payroll.PaymentAmount > 0)
        {
            return Math.Round(payroll.PaymentAmount / 160m, 2);
        }

        return payroll.PaymentAmount;
    }

    private static decimal CalculateLaborCost(IReadOnlyList<ShiftEntity> shifts, IReadOnlyDictionary<int, Employee> employees)
    {
        decimal totalCost = 0m;
        foreach (var shift in shifts)
        {
            if (shift.EmployeeId == null || !employees.TryGetValue(shift.EmployeeId.Value, out var emp))
            {
                continue;
            }

            var duration = (decimal)Math.Max(0, (shift.EndTime - shift.StartTime).TotalHours);
            var rate = CalculateHourlyRate(emp.PayrollProfile);
            totalCost += rate * duration;
        }

        return Math.Round(totalCost, 2);
    }

    private static WeekDayCalendarStatus DetermineCoverageStatus(
        bool isDayOff,
        IReadOnlyList<ShiftEntity> dayShifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        if (isDayOff)
        {
            return WeekDayCalendarStatus.DimmedDayOff;
        }

        if (dayShifts.Count == 0)
        {
            return WeekDayCalendarStatus.NoAllocations;
        }

        if (HasOvertimeOrMisallocation(dayShifts, employees))
        {
            return WeekDayCalendarStatus.OvertimeOrMisallocation;
        }

        if (dayShifts.Any(s => s.EmployeeId == null || !s.IsPublished))
        {
            return WeekDayCalendarStatus.MissingResourcesOrUnpublished;
        }

        return WeekDayCalendarStatus.CoveredAndPublished;
    }

    private static bool HasOvertimeOrMisallocation(IReadOnlyList<ShiftEntity> shifts, IReadOnlyDictionary<int, Employee> employees)
    {
        return shifts.Any(s =>
            (s.EndTime - s.StartTime).TotalHours > 8.0 ||
            (s.EmployeeId != null &&
             employees.TryGetValue(s.EmployeeId.Value, out var emp) &&
             emp.JobRoleId != s.JobRoleId));
    }

    private static string BuildSummaryMessage(int totalOpen, int assigned, int unfillable)
    {
        if (totalOpen == 0)
        {
            return "No open blocks found to auto-fill for this date.";
        }

        return $"Auto-fill completed: {assigned} assigned, {unfillable} unfillable out of {totalOpen} open blocks.";
    }
}
