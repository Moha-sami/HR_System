using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;

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
        var jobSpec = new Specification<JobRole>().Where(j => j.Id == request.JobId);
        var job = await _jobRepository.FirstOrDefaultAsync(
            jobSpec,
            j => new { j.Id, j.Title },
            cancellationToken);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");

        var affectedSpec = new Specification<Employee>()
            .Where(e => e.JobRoleId == request.JobId && e.IsActive && !e.IsDeleted);
        var affected = await _employeeRepository.ListAsync(
            affectedSpec,
            e => new AffectedEmployeeDto(
                e.Id,
                (e.FirstName != null && e.LastName != null ? e.FirstName.Trim() + " " + e.LastName.Trim() : (e.FirstName ?? e.LastName ?? string.Empty).Trim()).Trim()
            ),
            cancellationToken);

        return new JobDeletionImpactDto(
            JobId: job.Id,
            JobTitle: job.Title,
            AssignedEmployeesCount: affected.Count,
            CanDeleteDirectly: affected.Count == 0,
            AffectedEmployees: affected
        );
    }
}
