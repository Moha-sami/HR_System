using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;

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

        var spec = new Specification<Employee>()
            .Include(nameof(Employee.Site), "JobRole.Department")
            .Where(e => !e.IsDeleted && e.JobRoleId == request.JobId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            spec.Where(e => (e.FirstName != null && e.FirstName.ToLower().Contains(term)) ||
                            (e.LastName != null && e.LastName.ToLower().Contains(term)) ||
                            (e.Email != null && e.Email.ToLower().Contains(term)) ||
                            (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(term)));
        }

        spec.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);

        var paged = await _employeeRepository.PagedAsync(spec, page, pageSize, cancellationToken);

        var items = paged.Items.Select(MapToItemDto).ToList();

        return new JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize, paged.TotalPages);
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
            string.IsNullOrWhiteSpace(e.EmployeeCode) ? "N/A" : e.EmployeeCode,
            fullName,
            string.IsNullOrWhiteSpace(e.Email) ? "N/A" : e.Email,
            e.JobRole?.Department?.Name ?? "N/A",
            e.Site?.SiteName ?? "N/A",
            e.JoinDate,
            e.ProfilePhotoUrl
        );
    }
}
