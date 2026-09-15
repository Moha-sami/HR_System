using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Employees.GetEmployees;

public record GetEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Department = null,
    string? Region = null,
    string? Search = null,
    string? Sort = null,
    string? SortDir = "desc"
) : IRequest<PaginatedEmployeeListDto>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PaginatedEmployeeListDto>
{
    private readonly IRepository<Employee> _employeeRepository;

    public GetEmployeesQueryHandler(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<PaginatedEmployeeListDto> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        // 1. Start from Specification<Employee> with Eager Loaded Navigations
        var spec = new Specification<Employee>()
            .Include("JobRole.Department")
            .Include("Site.Region")
            .Include(nameof(Employee.Role));

        // 2. Search Filter (translated to SQL)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            spec.Where(e =>
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                e.Email.Contains(search) ||
                e.EmployeeCode.Contains(search));
        }

        // 3. Department Filter (translated to SQL)
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var department = request.Department.Trim();
            if (int.TryParse(department, out var deptId))
            {
                spec.Where(e => e.JobRole != null && e.JobRole.DepartmentId == deptId);
            }
            else
            {
                spec.Where(e => e.JobRole != null && e.JobRole.Department != null && e.JobRole.Department.Name.Contains(department));
            }
        }

        // 4. Region Filter (translated to SQL)
        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            if (int.TryParse(region, out var regionId))
            {
                spec.Where(e => e.Site != null && e.Site.RegionId == regionId);
            }
            else
            {
                spec.Where(e => e.Site != null && e.Site.Region != null && e.Site.Region.Name.Contains(region));
            }
        }

        // 5. Sorting (translated to SQL)
        var isAsc = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var sortField = request.Sort?.Trim().ToLower();

        switch (sortField)
        {
            case "name":
                spec.OrderBy(e => e.FirstName, descending: !isAsc).ThenBy(e => e.LastName, descending: !isAsc);
                break;
            case "employeecode":
                spec.OrderBy(e => e.EmployeeCode, descending: !isAsc);
                break;
            case "email":
                spec.OrderBy(e => e.Email, descending: !isAsc);
                break;
            case "jobtitle":
                spec.OrderBy(e => e.JobRole != null ? e.JobRole.Title : string.Empty, descending: !isAsc);
                break;
            default:
                spec.OrderBy(e => e.JoinDate, descending: !isAsc);
                break;
        }

        // 6-8. Total Count + Pagination bounds + materialize only the requested page
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var pagedResult = await _employeeRepository.PagedAsync(spec, page, pageSize, cancellationToken);
        var totalCount = pagedResult.TotalCount;
        var pagedEmployees = pagedResult.Items;

        // 9. Map materialized page to DTOs
        var items = pagedEmployees.Select(e =>
        {
            var jobTitle = e.JobRole?.Title ?? "N/A";
            var isAdmin = e.Role != null && (
                e.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                e.Role.Name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
            var fullName = $"{e.FirstName} {e.LastName}".Trim();

            return new EmployeeListRowDto(
                Id: e.Id,
                EmployeeCode: string.IsNullOrEmpty(e.EmployeeCode) ? $"EMP-{e.Id:D4}" : e.EmployeeCode,
                EmployeeName: fullName,
                JoinDate: e.JoinDate,
                JobTitle: jobTitle,
                Email: e.Email,
                AdminAccess: isAdmin
            );
        }).ToList();

        return new PaginatedEmployeeListDto(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }
}
