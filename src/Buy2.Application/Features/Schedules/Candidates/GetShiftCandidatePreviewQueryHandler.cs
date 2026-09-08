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

public class GetShiftCandidatePreviewQueryHandler : IRequestHandler<GetShiftCandidatePreviewQuery, ShiftCandidatePreviewDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;
    private readonly IRepository<PerformanceSubmission> _performanceRepository;
    private readonly IRepository<SitePreferredEmployee> _sitePreferredRepository;

    public GetShiftCandidatePreviewQueryHandler(
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

    public async Task<ShiftCandidatePreviewDto?> Handle(GetShiftCandidatePreviewQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .FirstOrDefaultAsync(e => e.Id == request.CandidateId && !e.IsDeleted, cancellationToken);

        if (employee == null)
        {
            return null;
        }

        // 1. Hourly Rate
        decimal hourlyRate = 0m;
        if (employee.PayrollProfile != null)
        {
            if (string.Equals(employee.PayrollProfile.SalaryType, "Monthly", StringComparison.OrdinalIgnoreCase) && employee.PayrollProfile.PaymentAmount > 0)
            {
                hourlyRate = Math.Round(employee.PayrollProfile.PaymentAmount / 160m, 2);
            }
            else
            {
                hourlyRate = employee.PayrollProfile.PaymentAmount;
            }
        }

        // 2. Rating
        var ratings = await _performanceRepository.Query(asNoTracking: true)
            .Where(p => p.EmployeeId == request.CandidateId)
            .Select(p => p.Score)
            .ToListAsync(cancellationToken);
        decimal rating = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : 5.0m;

        // 3. Career Completed Hours
        var careerCompletedHours = await _attendanceRepository.Query(asNoTracking: true)
            .Where(a => a.EmployeeId == request.CandidateId)
            .SumAsync(a => (decimal?)a.HoursWorked, cancellationToken) ?? 0m;

        // 4. Qualifications
        var qualifications = ParseJsonList(employee.JobRole?.RequiredQualificationsJson);

        // 5. Preferred for site
        var isPreferred = false;
        if (request.SiteId.HasValue)
        {
            isPreferred = await _sitePreferredRepository.Query(asNoTracking: true)
                .AnyAsync(spe => spe.SiteId == request.SiteId.Value && spe.EmployeeId == request.CandidateId, cancellationToken);
        }

        var employeeCode = string.IsNullOrEmpty(employee.EmployeeCode) ? $"EMP-{employee.Id:D4}" : employee.EmployeeCode;
        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();

        return new ShiftCandidatePreviewDto(
            Id: employee.Id,
            EmployeeCode: employeeCode,
            FullName: fullName,
            AvatarUrl: employee.ProfilePhotoUrl,
            JobTitle: employee.JobRole?.Title ?? string.Empty,
            JoinDate: employee.JoinDate,
            HourlyRate: hourlyRate,
            Rating: rating,
            CareerCompletedHours: careerCompletedHours,
            Qualifications: qualifications,
            IsPreferredForSite: isPreferred
        );
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
