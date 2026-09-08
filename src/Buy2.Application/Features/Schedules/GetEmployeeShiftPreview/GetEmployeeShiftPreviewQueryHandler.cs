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

namespace Buy2.Application.Features.Schedules.GetEmployeeShiftPreview;

public class GetEmployeeShiftPreviewQueryHandler : IRequestHandler<GetEmployeeShiftPreviewQuery, ShiftCandidatePreviewDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;
    private readonly IRepository<PerformanceSubmission> _performanceRepository;
    private readonly IRepository<SitePreferredEmployee> _sitePreferredRepository;
    private readonly IRepository<Qualification>? _qualificationRepository;

    public GetEmployeeShiftPreviewQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRepository,
        IRepository<PerformanceSubmission> performanceRepository,
        IRepository<SitePreferredEmployee> sitePreferredRepository)
        : this(employeeRepository, attendanceRepository, performanceRepository, sitePreferredRepository, null)
    {
    }

    public GetEmployeeShiftPreviewQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRepository,
        IRepository<PerformanceSubmission> performanceRepository,
        IRepository<SitePreferredEmployee> sitePreferredRepository,
        IRepository<Qualification>? qualificationRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _performanceRepository = performanceRepository;
        _sitePreferredRepository = sitePreferredRepository;
        _qualificationRepository = qualificationRepository;
    }

    public async Task<ShiftCandidatePreviewDto?> Handle(GetEmployeeShiftPreviewQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);

        if (employee == null)
        {
            return null;
        }

        var hourlyRate = CalculateHourlyRate(employee.PayrollProfile);
        var rating = await GetRatingScoreAsync(request.EmployeeId, cancellationToken);
        var careerCompletedHours = await GetCareerCompletedHoursAsync(request.EmployeeId, cancellationToken);
        var qualifications = await ResolveQualificationsAsync(employee.JobRole?.RequiredQualificationsJson, cancellationToken);
        var isPreferred = await CheckSitePreferenceAsync(request.SiteId, request.EmployeeId, cancellationToken);

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

    private async Task<decimal> GetRatingScoreAsync(int employeeId, CancellationToken cancellationToken)
    {
        var ratings = await _performanceRepository.Query(asNoTracking: true)
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => p.Score)
            .ToListAsync(cancellationToken);

        return ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : 5.0m;
    }

    private async Task<decimal> GetCareerCompletedHoursAsync(int employeeId, CancellationToken cancellationToken)
    {
        return await _attendanceRepository.Query(asNoTracking: true)
            .Where(a => a.EmployeeId == employeeId)
            .SumAsync(a => (decimal?)a.HoursWorked, cancellationToken) ?? 0m;
    }

    private async Task<List<string>> ResolveQualificationsAsync(string? json, CancellationToken cancellationToken)
    {
        var rawList = ParseJsonList(json);
        if (rawList.Count == 0 || _qualificationRepository == null)
        {
            return rawList;
        }

        var idList = new List<int>();
        var names = new List<string>();

        foreach (var item in rawList)
        {
            if (int.TryParse(item, out var id))
            {
                idList.Add(id);
            }
            else
            {
                names.Add(item);
            }
        }

        if (idList.Count == 0)
        {
            return names;
        }

        var dbNames = await _qualificationRepository.Query(asNoTracking: true)
            .Where(q => idList.Contains(q.Id))
            .Select(q => q.Name)
            .ToListAsync(cancellationToken);

        names.AddRange(dbNames);
        return names;
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            var list = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var val = element.ValueKind == JsonValueKind.String
                    ? element.GetString()
                    : element.ToString();

                if (!string.IsNullOrWhiteSpace(val))
                {
                    list.Add(val);
                }
            }

            return list;
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task<bool> CheckSitePreferenceAsync(int? siteId, int employeeId, CancellationToken cancellationToken)
    {
        if (!siteId.HasValue)
        {
            return false;
        }

        return await _sitePreferredRepository.Query(asNoTracking: true)
            .AnyAsync(spe => spe.SiteId == siteId.Value && spe.EmployeeId == employeeId, cancellationToken);
    }
}
