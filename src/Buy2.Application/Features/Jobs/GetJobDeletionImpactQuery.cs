using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Jobs;

public record GetJobDeletionImpactQuery(int JobId) : IRequest<JobDeletionImpactDto>;

public class GetJobDeletionImpactQueryHandler : IRequestHandler<GetJobDeletionImpactQuery, JobDeletionImpactDto>
{
    private readonly IRepository<JobRole> _jobRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetJobDeletionImpactQueryHandler(IRepository<JobRole> jobRepository, IRepository<Employee> employeeRepository)
    {
        _jobRepository = jobRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<JobDeletionImpactDto> Handle(GetJobDeletionImpactQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.Query(true)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");

        var activeEmployees = await _employeeRepository.Query(true)
            .Where(e => e.JobRoleId == request.JobId && e.IsActive && !e.IsDeleted)
            .ToListAsync(cancellationToken);


        var affected = activeEmployees.Select(e => new AffectedEmployeeDto(
            Id: e.Id,
            EmployeeCode: e.EmployeeCode,
            FullName: $"{e.FirstName?.Trim()} {e.LastName?.Trim()}".Trim(),
            Email: e.Email,
            SiteName: e.Site?.SiteName ?? string.Empty,
            ProfilePhotoUrl: e.ProfilePhotoUrl
        )).ToList();

        return new JobDeletionImpactDto(
            JobId: job.Id,
            JobTitle: job.Title,
            AssignedEmployeesCount: affected.Count,
            CanDeleteDirectly: affected.Count == 0,
            AffectedEmployees: affected
        );
    }
}

