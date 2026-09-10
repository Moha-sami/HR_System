using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.ShiftMarket;
using Buy2.Application.Features.ShiftTemplates;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftMarket.GetShiftMarketPostings;

public class GetShiftMarketPostingsQueryHandler : IRequestHandler<GetShiftMarketPostingsQuery, ShiftMarketPaginatedResponseDto>
{
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<ShiftClaim> _shiftClaimRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetShiftMarketPostingsQueryHandler(
        IRepository<ShiftEntity> shiftRepository,
        IRepository<ShiftClaim> shiftClaimRepository,
        IRepository<Site> siteRepository,
        IRepository<Employee> employeeRepository)
    {
        _shiftRepository = shiftRepository;
        _shiftClaimRepository = shiftClaimRepository;
        _siteRepository = siteRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<ShiftMarketPaginatedResponseDto> Handle(
        GetShiftMarketPostingsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = NormalizePagination(request.Page, request.PageSize);
        var shifts = await FetchPublishedShiftsAsync(request.SiteId, cancellationToken);
        if (shifts.Count == 0)
        {
            return new ShiftMarketPaginatedResponseDto([], 0, page, pageSize, 0);
        }

        var postings = await BuildPostingsAsync(shifts, cancellationToken);
        var filtered = FilterPostings(postings, request.Status, request.Search);
        var totalCount = filtered.Count;
        var totalPages = CalculateTotalPages(totalCount, pageSize);

        var pagedItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ShiftMarketPaginatedResponseDto(pagedItems, totalCount, page, pageSize, totalPages);
    }

    private async Task<List<ShiftEntity>> FetchPublishedShiftsAsync(int? siteId, CancellationToken cancellationToken)
    {
        var query = _shiftRepository.Query(true)
            .Include(s => s.ShiftTemplate)
                .ThenInclude(t => t!.LastUpdatedByEmployee)
            .Include(s => s.JobRole)
            .Where(s => s.IsPublished);

        if (siteId.HasValue)
        {
            query = query.Where(s => s.SiteId == siteId.Value);
        }

        return await query
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ShiftMarketPostingDto>> BuildPostingsAsync(
        List<ShiftEntity> shifts,
        CancellationToken cancellationToken)
    {
        var shiftIds = shifts.Select(s => s.Id).ToList();
        var claims = await _shiftClaimRepository.Query(true)
            .Where(c => shiftIds.Contains(c.ShiftId))
            .ToListAsync(cancellationToken);

        var claimsByShiftId = claims.GroupBy(c => c.ShiftId).ToDictionary(g => g.Key, g => g.ToList());

        var siteIds = shifts.Select(s => s.SiteId).Distinct().ToList();
        var sites = await _siteRepository.Query(true)
            .Where(s => siteIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.SiteName, cancellationToken);

        var claimantIds = claims.Select(c => c.EmployeeId).Distinct().ToList();
        var employees = await FetchEmployeesAsync(claimantIds, shifts, cancellationToken);
        var claimantAssignedShifts = await FetchClaimantAssignedShiftsAsync(claimantIds, cancellationToken);

        return shifts
            .Select(s => MapToPostingDto(
                s,
                claimsByShiftId.GetValueOrDefault(s.Id) ?? [],
                sites.GetValueOrDefault(s.SiteId) ?? string.Empty,
                employees,
                claimantAssignedShifts))
            .ToList();
    }

    private async Task<List<ShiftEntity>> FetchClaimantAssignedShiftsAsync(
        List<int> claimantIds,
        CancellationToken cancellationToken)
    {
        if (claimantIds.Count == 0)
        {
            return [];
        }

        return await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId.HasValue && claimantIds.Contains(s.EmployeeId.Value))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, Employee>> FetchEmployeesAsync(
        List<int> claimantIds,
        List<ShiftEntity> shifts,
        CancellationToken cancellationToken)
    {
        var assignedIds = shifts.Where(s => s.EmployeeId.HasValue).Select(s => s.EmployeeId!.Value);
        var allEmployeeIds = claimantIds.Union(assignedIds).Distinct().ToList();

        return await _employeeRepository.Query(true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .Where(e => allEmployeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private static ShiftMarketPostingDto MapToPostingDto(
        ShiftEntity shift,
        IReadOnlyList<ShiftClaim> claims,
        string siteName,
        IReadOnlyDictionary<int, Employee> employees,
        IReadOnlyList<ShiftEntity> assignedShifts)
    {
        var status = DeterminePostingStatus(shift, claims);
        var remainingHeadcount = status == "Covered" ? 0 : 1;

        var claimDtos = claims
            .Select(c => BuildClaimDto(shift, c, employees, assignedShifts.Where(s => s.EmployeeId == c.EmployeeId).ToList()))
            .OrderBy(c => c.ClaimSubmissionTimestamp)
            .ThenBy(c => c.ClaimId)
            .ToList();

        var startTime = shift.StartTime.TimeOfDay;
        var endTime = shift.EndTime.TimeOfDay;
        var formattedTiming = $"{ShiftTimeHelper.FormatTime(startTime)} - {ShiftTimeHelper.FormatTime(endTime)}";

        return new ShiftMarketPostingDto(
            ShiftId: shift.Id,
            SiteId: shift.SiteId,
            SiteName: siteName,
            PostingCreator: DeterminePostingCreator(shift.ShiftTemplate),
            ShiftTitle: DetermineShiftTitle(shift),
            Date: DateOnly.FromDateTime(shift.StartTime.Date),
            StartTime: startTime,
            EndTime: endTime,
            FormattedTiming: formattedTiming,
            JobRoleId: shift.JobRoleId,
            JobRoleTitle: shift.JobRole?.Title ?? string.Empty,
            RequiredHeadcount: 1,
            RemainingHeadcount: remainingHeadcount,
            TotalClaimRequestCount: claims.Count,
            Status: status,
            Claims: claimDtos
        );
    }

    private static ShiftMarketClaimDto BuildClaimDto(
        ShiftEntity shift,
        ShiftClaim claim,
        IReadOnlyDictionary<int, Employee> employees,
        IReadOnlyList<ShiftEntity> employeeAssignedShifts)
    {
        var employee = employees.GetValueOrDefault(claim.EmployeeId);
        var employeeName = employee != null
            ? $"{employee.FirstName} {employee.LastName}".Trim()
            : $"Employee #{claim.EmployeeId}";

        var projectedOvertime = employee != null
            ? CalculateProjectedOvertime(shift, employee, employeeAssignedShifts)
            : 0m;

        var timestamp = claim.CreatedAt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(claim.CreatedAt, TimeSpan.Zero)
            : claim.CreatedAt;

        return new ShiftMarketClaimDto(
            ClaimId: claim.Id,
            EmployeeId: claim.EmployeeId,
            EmployeeName: employeeName,
            ClaimSubmissionTimestamp: timestamp,
            ProjectedOvertimeHours: projectedOvertime,
            ClaimStatus: claim.Status,
            OvertimeJustification: claim.OvertimeJustification
        );
    }

    private static decimal CalculateProjectedOvertime(
        ShiftEntity shift,
        Employee employee,
        IReadOnlyList<ShiftEntity> employeeAssignedShifts)
    {
        var shiftDuration = (decimal)Math.Max(0, (shift.EndTime - shift.StartTime).TotalHours);
        var targetDate = DateOnly.FromDateTime(shift.StartTime.Date);
        var (weekStart, weekEnd) = GetWeekBoundary(targetDate);

        var weekShifts = employeeAssignedShifts
            .Where(s => s.Id != shift.Id && s.StartTime >= weekStart && s.StartTime < weekEnd)
            .ToList();

        var weeklyOvertime = CalculateWeeklyOvertime(weekShifts, shiftDuration, employee.PayrollProfile);
        var dailyOvertime = CalculateDailyOvertime(weekShifts, shiftDuration, targetDate);

        return Math.Max(weeklyOvertime, dailyOvertime);
    }

    private static decimal CalculateWeeklyOvertime(
        IReadOnlyList<ShiftEntity> weekShifts,
        decimal shiftDuration,
        PayrollProfile? profile)
    {
        var existingWeeklyHours = weekShifts.Sum(s => (decimal)Math.Max(0, (s.EndTime - s.StartTime).TotalHours));
        var totalWeeklyHours = existingWeeklyHours + shiftDuration;
        var threshold = profile?.OvertimeThresholdHours > 0 ? profile.OvertimeThresholdHours : 40.0m;

        return totalWeeklyHours > threshold ? Math.Round(totalWeeklyHours - threshold, 2) : 0m;
    }

    private static decimal CalculateDailyOvertime(
        IReadOnlyList<ShiftEntity> weekShifts,
        decimal shiftDuration,
        DateOnly targetDate)
    {
        var existingDailyHours = weekShifts
            .Where(s => DateOnly.FromDateTime(s.StartTime.Date) == targetDate)
            .Sum(s => (decimal)Math.Max(0, (s.EndTime - s.StartTime).TotalHours));

        var totalDailyHours = existingDailyHours + shiftDuration;
        return totalDailyHours > 8.0m ? Math.Round(totalDailyHours - 8.0m, 2) : 0m;
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

    private static string DeterminePostingCreator(ShiftTemplate? template)
    {
        if (template?.LastUpdatedByEmployee != null)
        {
            var name = $"{template.LastUpdatedByEmployee.FirstName} {template.LastUpdatedByEmployee.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return "Operations Manager";
    }

    private static string DetermineShiftTitle(ShiftEntity shift)
    {
        if (shift.ShiftTemplate != null && !string.IsNullOrWhiteSpace(shift.ShiftTemplate.Name))
        {
            return shift.ShiftTemplate.Name;
        }

        return ResolveRoleOrFallbackTitle(shift);
    }

    private static string ResolveRoleOrFallbackTitle(ShiftEntity shift)
    {
        if (shift.JobRole != null && !string.IsNullOrWhiteSpace(shift.JobRole.Title))
        {
            return shift.JobRole.Title;
        }

        return $"Shift #{shift.Id}";
    }

    private static string DeterminePostingStatus(ShiftEntity shift, IReadOnlyList<ShiftClaim> claims)
    {
        if (shift.EmployeeId != null || claims.Any(c => string.Equals(c.Status, "Approved", StringComparison.OrdinalIgnoreCase)))
        {
            return "Covered";
        }

        if (claims.Any(c => string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase)))
        {
            return "Pending";
        }

        return "Unclaimed";
    }

    private static List<ShiftMarketPostingDto> FilterPostings(
        List<ShiftMarketPostingDto> postings,
        string? statusFilter,
        string? search)
    {
        var trimmedSearch = search?.Trim();
        return postings
            .Where(p => MatchesStatus(p, statusFilter) && MatchesSearch(p, trimmedSearch))
            .ToList();
    }

    private static bool MatchesStatus(ShiftMarketPostingDto posting, string? statusFilter)
    {
        if (string.IsNullOrWhiteSpace(statusFilter) || string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(posting.Status, statusFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(ShiftMarketPostingDto posting, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var fields = new[] { posting.SiteName, posting.JobRoleTitle, posting.ShiftTitle, posting.PostingCreator };
        return fields.Any(f => f.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        return (normalizedPage, normalizedPageSize);
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        if (totalCount == 0 || pageSize <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((double)totalCount / pageSize);
    }
}
