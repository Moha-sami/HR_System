using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Jobs.GetJobEmployees;

public record GetJobEmployeesQuery(int JobId, int PageNumber = 1, int PageSize = 10) : IRequest<JobPaginatedResponseDto<JobEmployeeRosterItemDto>?>;

public class GetJobEmployeesQueryHandler : IRequestHandler<GetJobEmployeesQuery, JobPaginatedResponseDto<JobEmployeeRosterItemDto>?>
{
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetJobEmployeesQueryHandler(IRepository<JobRole> jobRoleRepository, IRepository<Employee> employeeRepository)
    {
        _jobRoleRepository = jobRoleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<JobPaginatedResponseDto<JobEmployeeRosterItemDto>?> Handle(GetJobEmployeesQuery request, CancellationToken cancellationToken)
    {
        var jobExists = await _jobRoleRepository.AnyAsync(j => j.Id == request.JobId, cancellationToken);
        if (!jobExists)
        {
            return null;
        }

        var page = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        IQueryable<Employee> query = _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.Site)
            .Include(e => e.JobRole)
                .ThenInclude(jr => jr!.Department)
            .Where(e => !e.IsDeleted && e.JobRoleId == request.JobId);

        var totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);

        var rawEmployees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawEmployees.Select(e => new JobEmployeeRosterItemDto(
            EmployeeId: e.Id,
            EmployeeCode: string.IsNullOrEmpty(e.EmployeeCode) ? $"EMP-{e.Id:D4}" : e.EmployeeCode,
            FullName: $"{e.FirstName} {e.LastName}".Trim(),
            Email: e.Email,
            Phone: e.PhoneNumber,
            SiteName: e.Site?.SiteName ?? "N/A",
            DepartmentName: e.JobRole?.Department?.Name ?? "N/A",
            HiredDate: e.JoinDate,
            Status: e.IsActive ? "Active" : "Inactive"
        )).ToList();

        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        return new JobPaginatedResponseDto<JobEmployeeRosterItemDto>(items, totalCount, page, pageSize, totalPages);
    }
}
