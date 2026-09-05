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

public record ReassignAndDeleteJobCommand(
    int JobId,
    int? DefaultReplacementJobId = null,
    List<EmployeeJobReassignmentDto>? Reassignments = null,
    int? ReplacementJobId = null
) : IRequest<ReassignAndDeleteJobResponseDto>;

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
        var fallbackJobId = request.DefaultReplacementJobId ?? request.ReplacementJobId;
        if (fallbackJobId.HasValue && fallbackJobId.Value == request.JobId)
            throw new ArgumentException("Replacement job ID cannot be the same as the target job ID.");

        var jobToDelete = await _jobRepository.Query(false)
            .Include(j => j.Employees)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (jobToDelete == null)
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found.");

        var activeEmployees = jobToDelete.Employees.Where(e => e.IsActive && !e.IsDeleted).ToList();
        int reassignedCount = activeEmployees.Count;

        if (activeEmployees.Any())
        {
            if (!fallbackJobId.HasValue && (request.Reassignments == null || !request.Reassignments.Any()))
                throw new InvalidOperationException("Cannot delete job role with assigned employees without a valid replacement job role.");

            await ReassignEmployeesAsync(request.JobId, activeEmployees, request.Reassignments, fallbackJobId, cancellationToken);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            jobToDelete.IsDeleted = true;
            jobToDelete.DeletedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ReassignAndDeleteJobResponseDto(
                Message: "Job role deleted successfully.",
                ReassignedCount: reassignedCount,
                DeletedJobId: request.JobId
            );
        }, cancellationToken);
    }

    private async Task ReassignEmployeesAsync(
        int currentJobId,
        List<Employee> activeEmployees,
        List<EmployeeJobReassignmentDto>? reassignments,
        int? fallbackJobId,
        CancellationToken cancellationToken)
    {
        var mapping = new Dictionary<int, int>();
        if (reassignments != null)
        {
            foreach (var item in reassignments)
            {
                if (item.NewJobId == currentJobId)
                    throw new ArgumentException($"Replacement job for employee {item.EmployeeId} cannot be the same as the target job ID.");

                mapping[item.EmployeeId] = item.NewJobId;
            }
        }

        // Validate that all active employees have a designated replacement job
        foreach (var employee in activeEmployees)
        {
            if (!mapping.ContainsKey(employee.Id))
            {
                if (fallbackJobId.HasValue)
                {
                    mapping[employee.Id] = fallbackJobId.Value;
                }
                else
                {
                    throw new InvalidOperationException($"Cannot delete job role: Employee '{employee.FirstName} {employee.LastName}' (ID {employee.Id}) has no valid replacement job role specified.");
                }
            }
        }

        // Verify all referenced target jobs exist
        var uniqueTargetJobIds = mapping.Values.Distinct().ToList();
        var existingJobs = await _jobRepository.Query(true)
            .Where(j => uniqueTargetJobIds.Contains(j.Id) && !j.IsDeleted)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        var missingJobs = uniqueTargetJobIds.Except(existingJobs).ToList();
        if (missingJobs.Any())
            throw new KeyNotFoundException($"Replacement job with ID {missingJobs.First()} not found.");

        // Apply reassignments
        foreach (var employee in activeEmployees)
        {
            employee.JobRoleId = mapping[employee.Id];
        }
    }
}
