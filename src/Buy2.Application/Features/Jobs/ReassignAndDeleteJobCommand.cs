using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

using Buy2.Application.Features.Jobs.DTOs;

namespace Buy2.Application.Features.Jobs;

public record ReassignAndDeleteJobCommand(int JobId, int? ReplacementJobId) : IRequest<ReassignAndDeleteJobResponseDto>;

public class ReassignAndDeleteJobCommandHandler : IRequestHandler<ReassignAndDeleteJobCommand, ReassignAndDeleteJobResponseDto>
{
    private readonly IRepository<JobRole> _jobRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReassignAndDeleteJobCommandHandler(
        IRepository<JobRole> jobRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReassignAndDeleteJobResponseDto> Handle(ReassignAndDeleteJobCommand request, CancellationToken cancellationToken)
    {
        if (request.JobId == request.ReplacementJobId)
            throw new ArgumentException("Replacement job ID cannot be the same as the target job ID.");

        var jobToDelete = await GetJobToDeleteAsync(request.JobId, cancellationToken);
        var activeEmployees = jobToDelete.Employees.Where(e => e.IsActive && !e.IsDeleted).ToList();

        if (activeEmployees.Any() && !request.ReplacementJobId.HasValue)
            throw new InvalidOperationException("Cannot delete job role with assigned employees without a valid replacement job role.");

        int reassignedCount = activeEmployees.Count;
        await ReassignEmployeesIfRequiredAsync(activeEmployees, request.ReplacementJobId);

        jobToDelete.IsDeleted = true;
        jobToDelete.DeletedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReassignAndDeleteJobResponseDto(
            Message: "Job role deleted successfully.",
            ReassignedCount: reassignedCount,
            DeletedJobId: request.JobId
        );
    }

    private async Task<JobRole> GetJobToDeleteAsync(int jobId, CancellationToken cancellationToken)
    {
        var jobToDelete = await _jobRepository.Query(false)
            .Include(j => j.Employees)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (jobToDelete == null)
            throw new KeyNotFoundException($"Job with ID {jobId} not found.");

        return jobToDelete;
    }

    private async Task ReassignEmployeesIfRequiredAsync(List<Employee> activeEmployees, int? replacementJobId)
    {
        if (!replacementJobId.HasValue)
            return;

        var replacementJob = await _jobRepository.GetByIdAsync(replacementJobId.Value);
        if (replacementJob == null)
            throw new KeyNotFoundException($"Replacement job with ID {replacementJobId.Value} not found.");

        foreach (var employee in activeEmployees)
        {
            employee.JobRoleId = replacementJobId.Value;
        }
    }
}
