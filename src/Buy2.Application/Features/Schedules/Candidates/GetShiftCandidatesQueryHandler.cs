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

namespace Buy2.Application.Features.Schedules.Candidates;

public class GetShiftCandidatesQueryHandler : IRequestHandler<GetShiftCandidatesQuery, PaginatedShiftCandidatesResponseDto>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;
    private readonly IRepository<PerformanceSubmission> _performanceRepository;
    private readonly IRepository<SitePreferredEmployee> _sitePreferredRepository;

    public GetShiftCandidatesQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRepository,
        IRepository<PerformanceSubmission> performanceRepository,
        IRepository<SitePreferredEmployee> sitePreferredRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _performanceRepository = performanceRepository;
        _sitePreferredRepository = sitePreferredRepository;
    }

    public async Task<PaginatedShiftCandidatesResponseDto> Handle(GetShiftCandidatesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        // 1. Query active, non-deleted employees with JobRole and PayrollProfile
        var employeesQuery = _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .Where(e => !e.IsDeleted && e.IsActive);

        // 2. Filter by Search (EmployeeCode, FirstName, LastName, FullName, or Id)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            employeesQuery = employeesQuery.Where(e =>
                e.EmployeeCode.Contains(search) ||
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                (e.FirstName + " " + e.LastName).Contains(search) ||
                e.Id.ToString() == search);
        }

        // 3. Filter by JobRoleIds
        if (request.JobRoleIds is { Count: > 0 })
        {
            employeesQuery = employeesQuery.Where(e => request.JobRoleIds.Contains(e.JobRoleId));
        }

        var candidates = await employeesQuery.ToListAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return new PaginatedShiftCandidatesResponseDto(new List<ShiftCandidateCardDto>(), 0, page, pageSize);
        }

        var candidateIds = candidates.Select(c => c.Id).ToList();

        // 4. Calculate Current Weekly Hours from AttendanceRecord (Date >= currentWeekStart, last 7 days)
        var currentWeekStart = DateTime.UtcNow.Date.AddDays(-7);
        var weeklyHoursByEmployee = await _attendanceRepository.Query(asNoTracking: true)
            .Where(a => candidateIds.Contains(a.EmployeeId) && a.Date >= currentWeekStart)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, TotalHours = g.Sum(a => a.HoursWorked) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.TotalHours, cancellationToken);

        // 5. Calculate Rating from PerformanceSubmission (average Score, default 5.0)
        var ratingByEmployee = await _performanceRepository.Query(asNoTracking: true)
            .Where(p => candidateIds.Contains(p.EmployeeId))
            .GroupBy(p => p.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, AvgScore = g.Average(p => p.Score) })
            .ToDictionaryAsync(g => g.EmployeeId, g => Math.Round(g.AvgScore, 1), cancellationToken);

        // 6. Check SitePreferredEmployee
        var preferredEmployeeIds = new HashSet<int>();
        if (request.SiteId.HasValue)
        {
            var preferredList = await _sitePreferredRepository.Query(asNoTracking: true)
                .Where(spe => spe.SiteId == request.SiteId.Value && candidateIds.Contains(spe.EmployeeId))
                .Select(spe => spe.EmployeeId)
                .ToListAsync(cancellationToken);
            preferredEmployeeIds = new HashSet<int>(preferredList);
        }

        // 7. Filter computed stats and build cards
        var targetQualifications = request.Qualifications?
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q.Trim())
            .ToList();

        var matchedCards = new List<ShiftCandidateCardDto>();

        foreach (var employee in candidates)
        {
            var isPreferred = request.SiteId.HasValue && preferredEmployeeIds.Contains(employee.Id);
            if (request.IsPreferredOnly == true && !isPreferred)
            {
                continue;
            }

            var currentWeeklyHours = weeklyHoursByEmployee.GetValueOrDefault(employee.Id, 0m);
            if (request.MinHours.HasValue && currentWeeklyHours < request.MinHours.Value)
            {
                continue;
            }
            if (request.MaxHours.HasValue && currentWeeklyHours > request.MaxHours.Value)
            {
                continue;
            }

            var ratingScore = ratingByEmployee.GetValueOrDefault(employee.Id, 5.0m);
            if (request.MinRating.HasValue && ratingScore < request.MinRating.Value)
            {
                continue;
            }
            if (request.MaxRating.HasValue && ratingScore > request.MaxRating.Value)
            {
                continue;
            }

            // Qualifications filter
            if (targetQualifications is { Count: > 0 })
            {
                var candidateQualifications = ParseJsonList(employee.JobRole?.RequiredQualificationsJson);
                var matchesQualification = targetQualifications.Any(tq =>
                    candidateQualifications.Any(cq => string.Equals(cq.Trim(), tq, StringComparison.OrdinalIgnoreCase)));
                if (!matchesQualification)
                {
                    continue;
                }
            }

            var employeeCode = string.IsNullOrEmpty(employee.EmployeeCode) ? $"EMP-{employee.Id:D4}" : employee.EmployeeCode;
            var fullName = $"{employee.FirstName} {employee.LastName}".Trim();

            matchedCards.Add(new ShiftCandidateCardDto(
                Id: employee.Id,
                EmployeeCode: employeeCode,
                FullName: fullName,
                AvatarUrl: employee.ProfilePhotoUrl,
                JobTitle: employee.JobRole?.Title ?? string.Empty,
                JobRoleId: employee.JobRoleId,
                CurrentWeeklyHours: currentWeeklyHours,
                RatingScore: ratingScore,
                IsPreferredForSite: isPreferred
            ));
        }

        var totalCount = matchedCards.Count;
        var pagedItems = matchedCards
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedShiftCandidatesResponseDto(pagedItems, totalCount, page, pageSize);
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
