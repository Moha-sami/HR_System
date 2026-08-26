using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Jobs.GetJobEmployees;

public record GetJobEmployeesQuery(int JobId, string? SearchTerm = null, int PageNumber = 1, int PageSize = 10) : IRequest<JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>?>;

public class GetJobEmployeesQueryHandler : IRequestHandler<GetJobEmployeesQuery, JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>?>
{
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetJobEmployeesQueryHandler(IRepository<JobRole> jobRoleRepository, IRepository<Employee> employeeRepository)
    {
        _jobRoleRepository = jobRoleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>?> Handle(GetJobEmployeesQuery request, CancellationToken cancellationToken)
    {
        var jobExists = await _jobRoleRepository.AnyAsync(j => j.Id == request.JobId, cancellationToken);
        if (!jobExists)
        {
            return null;
        }

        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.Site)
            .Include(e => e.JobRole)
                .ThenInclude(jr => jr!.Department)
            .Where(e => !e.IsDeleted && e.JobRoleId == request.JobId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(e => (e.FirstName != null && e.FirstName.ToLower().Contains(term)) ||
                                     (e.LastName != null && e.LastName.ToLower().Contains(term)) ||
                                     (e.Email != null && e.Email.ToLower().Contains(term)) ||
                                     (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawList = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawList.Select(MapToItemDto).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>(items, totalCount, page, pageSize, totalPages);
    }

    private static JobAssignedEmployeeListItemDto MapToItemDto(Employee e)
    {
        var fullName = $"{e.FirstName} {e.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = "N/A";
        }

        return new JobAssignedEmployeeListItemDto(
            e.Id,
            e.EmployeeCode ?? "N/A",
            fullName,
            e.Email ?? "N/A",
            e.JobRole?.Department?.Name ?? "N/A",
            e.Site?.SiteName ?? "N/A",
            e.JoinDate,
            e.ProfilePhotoUrl
        );
    }
}
