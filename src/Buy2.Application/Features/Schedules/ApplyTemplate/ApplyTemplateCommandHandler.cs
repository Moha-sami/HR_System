using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.ApplyTemplate.Services;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Schedules.ApplyTemplate;

public class ApplyTemplateCommandHandler : IRequestHandler<ApplyTemplateCommand, Result<ApplyTemplateResponseDto>>
{
    private readonly ITemplateApplicationLoader _loader;
    private readonly IAvailabilityResolver _availability;
    private readonly IEligibilityEvaluator _eligibility;
    private readonly IOverlapResolver _overlaps;
    private readonly IScheduleAnalyticsService _analytics;

    public ApplyTemplateCommandHandler(
        ITemplateApplicationLoader loader,
        IAvailabilityResolver availability,
        IEligibilityEvaluator eligibility,
        IOverlapResolver overlaps,
        IScheduleAnalyticsService analytics)
    {
        _loader = loader;
        _availability = availability;
        _eligibility = eligibility;
        _overlaps = overlaps;
        _analytics = analytics;
    }

    /// <summary>
    /// Backward-compatibility ctor for existing call sites / tests that
    /// construct the handler with repositories directly. Composes the
    /// focused services internally. Prefer the 5-service ctor via DI.
    /// </summary>
    [Obsolete("Use the (ITemplateApplicationLoader, IAvailabilityResolver, IEligibilityEvaluator, IOverlapResolver, IScheduleAnalyticsService) ctor via DI.")]
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
        : this(
            new TemplateApplicationLoader(
                siteRepository, templateRepository, shiftRepository,
                employeeRepository, jobRoleRepository, unitOfWork),
            new AvailabilityResolver(
                employeeSiteRepository, operationalHourRepository,
                requestRepository, attendanceRepository),
            new EligibilityEvaluator(),
            new OverlapResolver(shiftRepository),
            new ScheduleAnalyticsService())
    {
    }

    public async Task<Result<ApplyTemplateResponseDto>> Handle(
        ApplyTemplateCommand request, CancellationToken cancellationToken)
    {
        var keep = _overlaps.ParseKeepMode(request.Keep);
        if (keep == null)
        {
            return Result<ApplyTemplateResponseDto>.ValidationFailure(
                "Invalid keep value. Supported values: new, existing.");
        }

        var site = await _loader.LoadSiteAsync(request.SiteId, cancellationToken);
        if (site == null)
        {
            return Result<ApplyTemplateResponseDto>.NotFound($"Site with ID {request.SiteId} was not found.");
        }

        var template = await _loader.LoadTemplateAsync(request.TemplateId, cancellationToken);
        if (template == null)
        {
            return Result<ApplyTemplateResponseDto>.NotFound($"Shift template with ID {request.TemplateId} was not found.");
        }

        var blocks = template.ShiftBlocks.OrderBy(b => b.StartTime).ToList();
        if (blocks.Count == 0)
        {
            return Result<ApplyTemplateResponseDto>.ValidationFailure("Shift template has no shift blocks to apply.");
        }

        var existingShifts = await _loader.LoadDayShiftsAsync(request.SiteId, request.Date, cancellationToken);
        var employees = await _loader.LoadEmployeesAsync(blocks, cancellationToken);
        var roleTitles = await _loader.LoadRoleTitlesAsync(blocks, cancellationToken);
        var availability = await _availability.ResolveAsync(
            request.SiteId, site, request.Date, employees.Keys, cancellationToken);

        var planned = _eligibility.BuildPlannedBlocks(
            request.SiteId, request.Date, template.Id, blocks, employees, roleTitles, availability);
        _overlaps.DetectOverlaps(planned, existingShifts);
        var deletedIds = await _overlaps.ApplyKeepResolutionAsync(planned, keep.Value, cancellationToken);

        await _loader.PersistAsync(planned, cancellationToken);

        var finalDay = existingShifts.Where(s => !deletedIds.Contains(s.Id))
            .Concat(planned.Where(p => !p.Pruned).Select(p => p.Entity))
            .ToList();
        var costEmployees = await _loader.LoadCostEmployeesAsync(finalDay, employees, cancellationToken);
        var weekHours = await _loader.LoadWeeklyHoursBeforeDayAsync(finalDay, request.Date, cancellationToken);
        var laborCost = _analytics.CalculateLaborCost(finalDay, costEmployees, weekHours);
        var coverage = _analytics.DetermineCoverageStatus(availability.IsDayOff, finalDay, costEmployees);

        return Result<ApplyTemplateResponseDto>.Success(
            BuildResponse(request, planned, laborCost, coverage));
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
                .Select(MapWarning)
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

    private static ApplyTemplateWarningDto MapWarning(PlannedBlock block)
    {
        return new ApplyTemplateWarningDto(
            EmployeeId: block.SourceEmployeeId,
            EmployeeName: block.SourceEmployeeName ?? "Unknown",
            Role: block.RoleTitle,
            Reason: block.StripReason ?? string.Empty,
            Code: block.StripCode ?? string.Empty);
    }
}
