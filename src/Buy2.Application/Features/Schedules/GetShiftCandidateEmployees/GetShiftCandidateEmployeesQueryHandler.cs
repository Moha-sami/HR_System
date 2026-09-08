using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.GetShiftCandidateEmployees;

public class GetShiftCandidateEmployeesQueryHandler : IRequestHandler<GetShiftCandidateEmployeesQuery, PaginatedShiftCandidateEmployeesResponseDto>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;
    private readonly IRepository<PerformanceSubmission> _performanceRepository;
    private readonly IRepository<SitePreferredEmployee> _sitePreferredRepository;
    private readonly IRepository<Qualification> _qualificationRepository;

    public GetShiftCandidateEmployeesQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRepository,
        IRepository<PerformanceSubmission> performanceRepository,
        IRepository<SitePreferredEmployee> sitePreferredRepository,
        IRepository<Qualification> qualificationRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _performanceRepository = performanceRepository;
        _sitePreferredRepository = sitePreferredRepository;
        _qualificationRepository = qualificationRepository;
    }

    public async Task<PaginatedShiftCandidateEmployeesResponseDto> Handle(
        GetShiftCandidateEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = NormalizePaging(request.Page, request.PageSize);

        var employeesQuery = _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.JobRole)
            .Where(e => !e.IsDeleted && e.IsActive);

        employeesQuery = ApplyEmployeeFilters(employeesQuery, request.Search, request.RoleIds);

        var candidates = await employeesQuery.ToListAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return new PaginatedShiftCandidateEmployeesResponseDto(new List<ShiftCandidateEmployeeItemDto>(), 0, page, pageSize);
        }

        var candidateIds = candidates.Select(c => c.Id).ToList();

        var weeklyHoursByEmployee = await GetWeeklyHoursAsync(candidateIds, cancellationToken);
        var ratingByEmployee = await GetRatingsAsync(candidateIds, cancellationToken);
        var preferredEmployeeIds = await GetPreferredEmployeeIdsAsync(request.SiteId, candidateIds, cancellationToken);
        var (targetQualNames, targetQualIds) = await GetTargetQualificationsAsync(request.QualificationIds, cancellationToken);

        var matchedItems = FilterAndMapCandidates(
            candidates,
            request,
            weeklyHoursByEmployee,
            ratingByEmployee,
            preferredEmployeeIds,
            targetQualNames,
            targetQualIds);

        var totalCount = matchedItems.Count;
        var pagedItems = matchedItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedShiftCandidateEmployeesResponseDto(pagedItems, totalCount, page, pageSize);
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        var normalizedPage = page > 0 ? page : 1;
        var normalizedPageSize = pageSize > 0 ? Math.Min(pageSize, 100) : 10;
        return (normalizedPage, normalizedPageSize);
    }

    private static IQueryable<Employee> ApplyEmployeeFilters(
        IQueryable<Employee> query,
        string? search,
        List<int>? roleIds)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            var lowerSearch = trimmedSearch.ToLower();
            query = query.Where(e =>
                (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(lowerSearch)) ||
                (e.FirstName != null && e.FirstName.ToLower().Contains(lowerSearch)) ||
                (e.LastName != null && e.LastName.ToLower().Contains(lowerSearch)) ||
                ((e.FirstName + " " + e.LastName).ToLower().Contains(lowerSearch)) ||
                e.Id.ToString() == trimmedSearch);
        }

        if (roleIds is { Count: > 0 })
        {
            query = query.Where(e => roleIds.Contains(e.JobRoleId));
        }

        return query;
    }

    private async Task<Dictionary<int, decimal>> GetWeeklyHoursAsync(
        List<int> candidateIds,
        CancellationToken cancellationToken)
    {
        var currentWeekStart = DateTime.UtcNow.Date.AddDays(-7);
        return await _attendanceRepository.Query(asNoTracking: true)
            .Where(a => candidateIds.Contains(a.EmployeeId) && a.Date >= currentWeekStart)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, TotalHours = g.Sum(a => a.HoursWorked) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.TotalHours, cancellationToken);
    }

    private async Task<Dictionary<int, decimal>> GetRatingsAsync(
        List<int> candidateIds,
        CancellationToken cancellationToken)
    {
        return await _performanceRepository.Query(asNoTracking: true)
            .Where(p => candidateIds.Contains(p.EmployeeId))
            .GroupBy(p => p.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, AvgScore = g.Average(p => p.Score) })
            .ToDictionaryAsync(g => g.EmployeeId, g => Math.Round(g.AvgScore, 2), cancellationToken);
    }

    private async Task<HashSet<int>> GetPreferredEmployeeIdsAsync(
        int? siteId,
        List<int> candidateIds,
        CancellationToken cancellationToken)
    {
        if (!siteId.HasValue)
        {
            return new HashSet<int>();
        }

        var preferredList = await _sitePreferredRepository.Query(asNoTracking: true)
            .Where(spe => spe.SiteId == siteId.Value && candidateIds.Contains(spe.EmployeeId))
            .Select(spe => spe.EmployeeId)
            .ToListAsync(cancellationToken);

        return preferredList.ToHashSet();
    }

    private async Task<(HashSet<string> Names, HashSet<string> Ids)> GetTargetQualificationsAsync(
        List<int>? qualificationIds,
        CancellationToken cancellationToken)
    {
        if (qualificationIds is not { Count: > 0 })
        {
            return (new HashSet<string>(), new HashSet<string>());
        }

        var idStrings = qualificationIds.Select(id => id.ToString()).ToHashSet();

        var qualNames = await _qualificationRepository.Query(asNoTracking: true)
            .Where(q => qualificationIds.Contains(q.Id))
            .Select(q => q.Name.Trim().ToLowerInvariant())
            .ToListAsync(cancellationToken);

        return (qualNames.ToHashSet(), idStrings);
    }

    private static List<ShiftCandidateEmployeeItemDto> FilterAndMapCandidates(
        List<Employee> candidates,
        GetShiftCandidateEmployeesQuery request,
        Dictionary<int, decimal> weeklyHoursByEmployee,
        Dictionary<int, decimal> ratingByEmployee,
        HashSet<int> preferredEmployeeIds,
        HashSet<string> targetQualNames,
        HashSet<string> targetQualIds)
    {
        var matchedItems = new List<ShiftCandidateEmployeeItemDto>();

        foreach (var employee in candidates)
        {
            var isPreferred = request.SiteId.HasValue && preferredEmployeeIds.Contains(employee.Id);
            if (request.IsPreferredOnly == true && !isPreferred)
            {
                continue;
            }

            var weeklyHours = weeklyHoursByEmployee.GetValueOrDefault(employee.Id, 0m);
            if (!FilterByHours(weeklyHours, request.MinHours, request.MaxHours))
            {
                continue;
            }

            var ratingScore = ratingByEmployee.GetValueOrDefault(employee.Id, 0m);
            if (!FilterByRatingTiers(ratingScore, request.RatingTiers))
            {
                continue;
            }

            var candidateQuals = ParseJsonList(employee.JobRole?.RequiredQualificationsJson);
            if (!MatchesQualifications(candidateQuals, targetQualNames, targetQualIds))
            {
                continue;
            }

            var employeeCode = string.IsNullOrEmpty(employee.EmployeeCode) ? $"EMP-{employee.Id:D4}" : employee.EmployeeCode;
            var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
            var roleTitle = employee.JobRole?.Title ?? string.Empty;
            var riskToken = DetermineRiskStatusToken(ratingScore, weeklyHours);

            matchedItems.Add(new ShiftCandidateEmployeeItemDto(
                Id: employee.Id,
                EmployeeCode: employeeCode,
                FullName: fullName,
                RoleTitle: roleTitle,
                JobRoleId: employee.JobRoleId,
                WeeklyCompletedHours: weeklyHours,
                RatingScore: ratingScore,
                RiskStatusToken: riskToken,
                IsPreferredForSite: isPreferred
            ));
        }

        return matchedItems;
    }

    private static bool FilterByHours(decimal hours, decimal? minHours, decimal? maxHours)
    {
        if (minHours.HasValue && hours < minHours.Value)
        {
            return false;
        }
        if (maxHours.HasValue && hours > maxHours.Value)
        {
            return false;
        }
        return true;
    }

    private static bool FilterByRatingTiers(decimal rating, List<string>? ratingTiers)
    {
        if (ratingTiers is not { Count: > 0 })
        {
            return true;
        }

        return ratingTiers.Any(tier => MatchesRatingTier(rating, tier));
    }

    private static bool MatchesRatingTier(decimal rating, string tier)
    {
        var normalized = tier.Trim().ToLowerInvariant();
        return normalized switch
        {
            "4.5+" => rating >= 4.5m,
            "4.0-4.49" => rating >= 4.0m && rating < 4.5m,
            "3.0-3.99" => rating >= 3.0m && rating < 4.0m,
            "2.0-2.99" => rating >= 2.0m && rating < 3.0m,
            "below 2.0" or "<2.0" => rating < 2.0m && rating > 0m,
            "unrated" => rating <= 0m,
            _ => false
        };
    }

    public static string DetermineRiskStatusToken(decimal ratingScore, decimal weeklyCompletedHours)
    {
        if (ratingScore >= 4.5m)
        {
            return "TopPerformer";
        }
        if (weeklyCompletedHours >= 35m)
        {
            return "OvertimeRisk";
        }
        if (ratingScore < 2.0m && ratingScore > 0m)
        {
            return "Warning";
        }
        return "Normal";
    }

    private static bool MatchesQualifications(
        List<string> candidateQuals,
        HashSet<string> targetQualNames,
        HashSet<string> targetQualIds)
    {
        if (targetQualNames.Count == 0 && targetQualIds.Count == 0)
        {
            return true;
        }

        return candidateQuals.Any(cq =>
            targetQualNames.Contains(cq.Trim().ToLowerInvariant()) ||
            targetQualIds.Contains(cq.Trim()));
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
