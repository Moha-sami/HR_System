using Buy2.Application.Common.Helpers;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.ApplyTemplate;

public class ApplyTemplateCommandHandler : IRequestHandler<ApplyTemplateCommand, Result<ApplyTemplateResponseDto>>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftTemplate> _templateRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<EmployeeSite> _employeeSiteRepository;
    private readonly IRepository<SiteOperationalHour> _operationalHourRepository;
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyTemplateCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftTemplate> templateRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<EmployeeSite> employeeSiteRepository,
        IRepository<SiteOperationalHour> operationalHourRepository,
        IRepository<Request> requestRepository,
        IRepository<AttendanceRecord> attendanceRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _templateRepository = templateRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _jobRoleRepository = jobRoleRepository;
        _employeeSiteRepository = employeeSiteRepository;
        _operationalHourRepository = operationalHourRepository;
        _requestRepository = requestRepository;
        _attendanceRepository = attendanceRepository;
        _unitOfWork = unitOfWork;
    }

    private enum KeepMode
    {
        None,
        KeepNew,
        KeepExisting
    }

    private sealed record StripDecision(string Code, string Reason);

    private sealed class PlannedBlock
    {
        public ShiftEntity Entity { get; set; } = null!;
        public string RoleTitle { get; set; } = string.Empty;
        public int? SourceEmployeeId { get; set; }
        public string? SourceEmployeeName { get; set; }
        public string? AssignedEmployeeName { get; set; }
        public bool Stripped { get; set; }
        public string? StripCode { get; set; }
        public string? StripReason { get; set; }
        public bool Collision { get; set; }
        public string? CollisionType { get; set; }
        public bool Conflict { get; set; }
        public string? ConflictType { get; set; }
        public bool Pruned { get; set; }
        public HashSet<int> OverlappingExistingIds { get; } = new();
    }

    private sealed class AvailabilityContext
    {
        public HashSet<int> AuthorizedEmployeeIds { get; set; } = new();
        public bool IsDayOff { get; set; }
        public SiteOperationalHour? DayHours { get; set; }
        public List<Request> LeaveRequests { get; set; } = new();
        public HashSet<int> LeaveRecordEmployeeIds { get; set; } = new();
        public Dictionary<int, string> LeaveTypeByEmployeeId { get; set; } = new();
        public Dictionary<int, bool> RemoteWorkByEmployeeId { get; set; } = new();
    }

    public async Task<Result<ApplyTemplateResponseDto>> Handle(
        ApplyTemplateCommand request, CancellationToken cancellationToken)
    {
        var keep = ParseKeepMode(request.Keep);
        if (keep == null)
        {
            return Result<ApplyTemplateResponseDto>.ValidationFailure(
                "Invalid keep value. Supported values: new, existing.");
        }

        var site = await LoadSiteAsync(request.SiteId, cancellationToken);
        if (site == null)
        {
            return Result<ApplyTemplateResponseDto>.NotFound($"Site with ID {request.SiteId} was not found.");
        }

        var template = await LoadTemplateAsync(request.TemplateId, cancellationToken);
        if (template == null)
        {
            return Result<ApplyTemplateResponseDto>.NotFound($"Shift template with ID {request.TemplateId} was not found.");
        }

        var blocks = template.ShiftBlocks.OrderBy(b => b.StartTime).ToList();
        if (blocks.Count == 0)
        {
            return Result<ApplyTemplateResponseDto>.ValidationFailure("Shift template has no shift blocks to apply.");
        }

        var existingShifts = await LoadDayShiftsAsync(request.SiteId, request.Date, cancellationToken);
        var employees = await LoadEmployeesAsync(blocks, cancellationToken);
        var roleTitles = await LoadRoleTitlesAsync(blocks, cancellationToken);
        var availability = await BuildAvailabilityAsync(
            request.SiteId, site, request.Date, employees.Keys, cancellationToken);

        var planned = BuildPlannedBlocks(request, template.Id, blocks, employees, roleTitles, availability);
        DetectOverlaps(planned, FindOverlapCandidates(planned, existingShifts));
        var deletedIds = ApplyKeepResolution(planned, keep.Value);

        await PersistAsync(planned, cancellationToken);

        var finalDay = existingShifts.Where(s => !deletedIds.Contains(s.Id))
            .Concat(planned.Where(p => !p.Pruned).Select(p => p.Entity))
            .ToList();
        var costEmployees = await LoadCostEmployeesAsync(finalDay, employees, cancellationToken);
        var weekHours = await LoadWeeklyHoursBeforeDayAsync(finalDay, request.Date, cancellationToken);
        var laborCost = CalculateLaborCost(finalDay, costEmployees, weekHours);
        var coverage = DetermineCoverageStatus(availability.IsDayOff, finalDay, costEmployees);

        return Result<ApplyTemplateResponseDto>.Success(
            BuildResponse(request, planned, laborCost, coverage));
    }

    private static KeepMode? ParseKeepMode(string? keep)
    {
        if (string.IsNullOrWhiteSpace(keep))
        {
            return KeepMode.None;
        }

        if (keep.Trim().Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            return KeepMode.KeepNew;
        }

        if (keep.Trim().Equals("existing", StringComparison.OrdinalIgnoreCase))
        {
            return KeepMode.KeepExisting;
        }

        return null;
    }

    private async Task<Site?> LoadSiteAsync(int siteId, CancellationToken cancellationToken)
    {
        return await _siteRepository.Query(true)
            .Include(s => s.OperationalHours)
            .FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
    }

    private async Task<ShiftTemplate?> LoadTemplateAsync(int templateId, CancellationToken cancellationToken)
    {
        return await _templateRepository.Query(true)
            .Include(t => t.ShiftBlocks)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }

    private async Task<List<ShiftEntity>> LoadDayShiftsAsync(
        int siteId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        return await _shiftRepository.Query(false)
            .Where(s => s.SiteId == siteId && s.StartTime < dayEnd && s.EndTime > dayStart)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, Employee>> LoadEmployeesAsync(
        List<ShiftBlock> blocks, CancellationToken cancellationToken)
    {
        var ids = blocks
            .Where(b => b.EmployeeId.HasValue)
            .Select(b => b.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, Employee>();
        }

        return await _employeeRepository.Query(true)
            .Include(e => e.PayrollProfile)
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private async Task<Dictionary<int, string>> LoadRoleTitlesAsync(
        List<ShiftBlock> blocks, CancellationToken cancellationToken)
    {
        var ids = blocks.Select(b => b.JobRoleId).Distinct().ToList();

        return await _jobRoleRepository.Query(true)
            .Where(r => ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Title, cancellationToken);
    }

    private async Task<AvailabilityContext> BuildAvailabilityAsync(
        int siteId,
        Site site,
        DateOnly date,
        IEnumerable<int> candidateIds,
        CancellationToken cancellationToken)
    {
        var ids = candidateIds.ToList();
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

        var authorized = await _employeeSiteRepository.Query(true)
            .Where(l => l.SiteId == siteId && ids.Contains(l.EmployeeId))
            .Select(l => l.EmployeeId)
            .ToListAsync(cancellationToken);

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

        var requests = await _requestRepository.Query(true)
            .Include(r => r.RequestType)
            .Where(r => ids.Contains(r.EmployeeId)
                && r.StartDate.HasValue && r.StartDate.Value < dayEnd
                && (!r.EndDate.HasValue || r.EndDate.Value >= dayStart))
            .ToListAsync(cancellationToken);

        foreach (var req in requests)
        {
            AddCoveringLeave(context, req, date);
        }

        var records = await _attendanceRepository.Query(true)
            .Where(a => ids.Contains(a.EmployeeId) && a.Date >= dayStart && a.Date < dayEnd)
            .ToListAsync(cancellationToken);

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
        var fromRepo = await _operationalHourRepository.Query(true)
            .Where(o => o.SiteId == site.Id)
            .ToListAsync(cancellationToken);

        if (fromRepo.Count > 0)
        {
            return fromRepo.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
        }

        var source = site.OperationalHours ?? Enumerable.Empty<SiteOperationalHour>();
        return source.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
    }

    private List<PlannedBlock> BuildPlannedBlocks(
        ApplyTemplateCommand request,
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
            var strip = ResolveStrip(block, employee, roleTitle, request.Date, availability);

            var start = ToDateTimeOffset(request.Date, block.StartTime);
            var end = ToDateTimeOffset(request.Date, block.EndTime);
            if (end <= start)
            {
                end = end.AddDays(1);
            }

            var entity = new ShiftEntity
            {
                SiteId = request.SiteId,
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

    private static List<ShiftEntity> FindOverlapCandidates(
        List<PlannedBlock> planned, List<ShiftEntity> existingShifts)
    {
        if (planned.Count == 0)
        {
            return new List<ShiftEntity>();
        }

        if (existingShifts.Count == 0)
        {
            return new List<ShiftEntity>();
        }

        var minStart = planned.Min(p => p.Entity.StartTime);
        var maxEnd = planned.Max(p => p.Entity.EndTime);

        return existingShifts
            .Where(s => s.StartTime < maxEnd && s.EndTime > minStart)
            .ToList();
    }

    private static void DetectOverlaps(List<PlannedBlock> planned, List<ShiftEntity> candidates)
    {
        var seen = new List<ShiftEntity>(candidates);
        var existingIds = candidates.Select(s => s.Id).ToHashSet();

        foreach (var block in planned)
        {
            FlagBlockOverlaps(block, seen, existingIds);
            seen.Add(block.Entity);
        }
    }

    private static void FlagBlockOverlaps(
        PlannedBlock block, List<ShiftEntity> seen, HashSet<int> existingIds)
    {
        foreach (var other in seen)
        {
            if (!Overlaps(block.Entity, other))
            {
                continue;
            }

            if (existingIds.Contains(other.Id))
            {
                block.OverlappingExistingIds.Add(other.Id);
            }

            FlagCollisionOrConflict(block, other);
        }
    }

    private static void FlagCollisionOrConflict(PlannedBlock block, ShiftEntity other)
    {
        if (block.Entity.EmployeeId.HasValue && other.EmployeeId == block.Entity.EmployeeId)
        {
            block.Collision = true;
            block.CollisionType = ApplyTemplateCollisionTypes.EmployeeOverlap;
        }
        else
        {
            block.Conflict = true;
            block.ConflictType = ApplyTemplateConflictTypes.RoleSlotConflict;
        }
    }

    private static bool Overlaps(ShiftEntity first, ShiftEntity second)
    {
        return first.StartTime < second.EndTime && second.StartTime < first.EndTime;
    }

    private HashSet<int> ApplyKeepResolution(List<PlannedBlock> planned, KeepMode keep)
    {
        var deletedIds = new HashSet<int>();

        if (keep == KeepMode.KeepNew)
        {
            DeleteOverlappingExisting(planned, deletedIds);
        }

        if (keep == KeepMode.KeepExisting)
        {
            PruneCollidingNew(planned);
        }

        return deletedIds;
    }

    private void DeleteOverlappingExisting(List<PlannedBlock> planned, HashSet<int> deletedIds)
    {
        var targets = planned
            .SelectMany(p => p.OverlappingExistingIds)
            .Distinct()
            .ToList();

        if (targets.Count == 0)
        {
            return;
        }

        foreach (var shift in _shiftRepository.Query(false).Where(s => targets.Contains(s.Id)).ToList())
        {
            _shiftRepository.Delete(shift);
            deletedIds.Add(shift.Id);
        }
    }

    private static void PruneCollidingNew(List<PlannedBlock> planned)
    {
        foreach (var block in planned)
        {
            if (block.Collision || block.Conflict)
            {
                block.Pruned = true;
            }
        }
    }

    private async Task PersistAsync(List<PlannedBlock> planned, CancellationToken cancellationToken)
    {
        foreach (var block in planned.Where(p => !p.Pruned))
        {
            await _shiftRepository.AddAsync(block.Entity, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<int, Employee>> LoadCostEmployeesAsync(
        List<ShiftEntity> finalDay,
        Dictionary<int, Employee> known,
        CancellationToken cancellationToken)
    {
        var missingIds = finalDay
            .Where(s => s.EmployeeId.HasValue && !known.ContainsKey(s.EmployeeId.Value))
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (missingIds.Count == 0)
        {
            return new Dictionary<int, Employee>(known);
        }

        var missing = await _employeeRepository.Query(true)
            .Include(e => e.PayrollProfile)
            .Where(e => missingIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        foreach (var entry in missing)
        {
            known[entry.Key] = entry.Value;
        }

        return known;
    }

    private static ApplyTemplateResponseDto BuildResponse(
        ApplyTemplateCommand request,
        List<PlannedBlock> planned,
        (decimal Total, decimal Regular, decimal Overtime) laborCost,
        WeekDayCalendarStatus coverage)
    {
        return new ApplyTemplateResponseDto(
            SiteId: request.SiteId,
            Date: request.Date,
            TemplateId: request.TemplateId,
            Blocks: planned.Select(MapBlock).ToList(),
            Warnings: planned
                .Where(p => p.Stripped)
                .Select(p => new ApplyTemplateWarningDto(
                    EmployeeId: p.SourceEmployeeId,
                    EmployeeName: p.SourceEmployeeName ?? "Unknown",
                    Role: p.RoleTitle,
                    Reason: p.StripReason ?? string.Empty,
                    Code: p.StripCode ?? string.Empty))
                .ToList(),
            TotalLaborCost: laborCost.Total,
            RegularCost: laborCost.Regular,
            OvertimeCost: laborCost.Overtime,
            CoverageStatus: coverage,
            PrunedCount: planned.Count(p => p.Pruned));
    }

    private static AppliedTemplateBlockDto MapBlock(PlannedBlock block)
    {
        return new AppliedTemplateBlockDto(
            ShiftId: block.Pruned ? 0 : block.Entity.Id,
            SiteId: block.Entity.SiteId,
            JobRoleId: block.Entity.JobRoleId,
            RoleTitle: block.RoleTitle,
            StartTime: block.Entity.StartTime,
            EndTime: block.Entity.EndTime,
            IsPublished: block.Entity.IsPublished,
            EmployeeId: block.Entity.EmployeeId,
            EmployeeName: block.AssignedEmployeeName,
            IsOpenRole: block.Entity.EmployeeId == null,
            Stripped: block.Stripped,
            StripCode: block.StripCode,
            StripReason: block.StripReason,
            Collision: block.Collision,
            CollisionType: block.CollisionType,
            Conflict: block.Conflict,
            ConflictType: block.ConflictType,
            Pruned: block.Pruned);
    }

    private async Task<Dictionary<int, decimal>> LoadWeeklyHoursBeforeDayAsync(
        List<ShiftEntity> finalDay, DateOnly date, CancellationToken cancellationToken)
    {
        var ids = finalDay
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var (weekStart, _) = GetWeekBoundary(date);
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var shifts = await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId != null
                && ids.Contains(s.EmployeeId.Value)
                && s.StartTime >= weekStart
                && s.StartTime < dayStart)
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => shifts
                .Where(s => s.EmployeeId == id)
                .Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours));
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

    private static (decimal Total, decimal Regular, decimal Overtime) CalculateLaborCost(
        IReadOnlyList<ShiftEntity> dayShifts,
        IReadOnlyDictionary<int, Employee> employees,
        IReadOnlyDictionary<int, decimal> weekHoursBefore)
    {
        decimal regular = 0m;
        decimal overtime = 0m;

        var assigned = dayShifts.Where(s => s.EmployeeId.HasValue).GroupBy(s => s.EmployeeId!.Value);
        foreach (var group in assigned)
        {
            if (!employees.TryGetValue(group.Key, out var emp))
            {
                continue;
            }

            PriceEmployeeDay(group, emp, weekHoursBefore, ref regular, ref overtime);
        }

        return (Math.Round(regular + overtime, 2), Math.Round(regular, 2), Math.Round(overtime, 2));
    }

    private static void PriceEmployeeDay(
        IEnumerable<ShiftEntity> dayShifts,
        Employee employee,
        IReadOnlyDictionary<int, decimal> weekHoursBefore,
        ref decimal regular,
        ref decimal overtime)
    {
        var rate = CalculateHourlyRate(employee.PayrollProfile);
        var overtimeRate = employee.PayrollProfile?.OvertimeHourlyRate > 0
            ? employee.PayrollProfile.OvertimeHourlyRate
            : rate * 1.5m;
        var threshold = employee.PayrollProfile?.OvertimeThresholdHours > 0
            ? employee.PayrollProfile.OvertimeThresholdHours
            : 40m;
        var cumulative = weekHoursBefore.TryGetValue(employee.Id, out var before) ? before : 0m;

        foreach (var shift in dayShifts.OrderBy(s => s.StartTime))
        {
            var duration = (decimal)Math.Max(0, (shift.EndTime - shift.StartTime).TotalHours);
            var regularHours = Math.Max(0, Math.Min(duration, threshold - cumulative));
            regular += regularHours * rate;
            overtime += (duration - regularHours) * overtimeRate;
            cumulative += duration;
        }
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

    private static bool HasOvertimeOrMisallocation(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        return shifts.Any(s =>
            (s.EndTime - s.StartTime).TotalHours > 8.0 ||
            (s.EmployeeId != null &&
             employees.TryGetValue(s.EmployeeId.Value, out var emp) &&
             emp.JobRoleId != s.JobRoleId));
    }
}
