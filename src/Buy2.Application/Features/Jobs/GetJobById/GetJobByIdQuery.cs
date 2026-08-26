using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Jobs.GetJobById;

public record GetJobByIdQuery(int Id) : IRequest<JobDetailsDto?>;

public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDetailsDto?>
{
    private readonly IRepository<JobRole> _jobRoleRepository;

    public GetJobByIdQueryHandler(IRepository<JobRole> jobRoleRepository)
    {
        _jobRoleRepository = jobRoleRepository;
    }

    public async Task<JobDetailsDto?> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRoleRepository.Query(asNoTracking: true)
            .Include(j => j.Department)
            .Include(j => j.Employees)
            .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

        if (job == null)
        {
            return null;
        }

        return MapToJobDetailsDto(job);
    }

    private static JobDetailsDto MapToJobDetailsDto(JobRole job)
    {
        return new JobDetailsDto(
            job.Id,
            job.Title,
            job.DepartmentId,
            job.Department?.Name ?? "N/A",
            job.SeniorityLevel,
            job.Description,
            ParseJsonList(job.RequiredQualificationsJson),
            job.ExperienceYears,
            job.AttendanceType,
            ParseJsonList(job.OnlineWorkdaysJson),
            ParseJsonList(job.OfflineWorkdaysJson),
            job.Employees?.Count(e => !e.IsDeleted) ?? 0,
            job.IsActive,
            job.CreatedAt,
            job.UpdatedAt
        );
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
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
