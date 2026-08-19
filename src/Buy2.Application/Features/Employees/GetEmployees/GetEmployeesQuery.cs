using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        // 1. Start from IQueryable<Employee> with Eager Loaded Navigations
        IQueryable<Employee> query = _employeeRepository.Query()
            .Include(e => e.JobRole)
            .Include(e => e.Site)
            .Include(e => e.Role);

        // 2. Search Filter (translated to SQL)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(e =>
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                e.Email.Contains(search) ||
                e.EmployeeCode.Contains(search));
        }

        // 3. Department Filter (translated to SQL)
        // Note: Domain currently models departments via JobRole.DepartmentId (no dedicated Department entity exists yet).
        // Numeric input filters strictly by DepartmentId; text input matches against JobRole.Title.
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var department = request.Department.Trim();
            query = int.TryParse(department, out var deptId)
                ? query.Where(e => e.JobRole != null && e.JobRole.DepartmentId == deptId)
                : query.Where(e => e.JobRole != null && e.JobRole.Title.Contains(department));
        }

        // 4. Region Filter (translated to SQL)
        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            query = query.Where(e => e.Site != null && e.Site.SiteName.Contains(region));
        }

        // 5. Sorting (translated to SQL)
        var isAsc = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var sortField = request.Sort?.Trim().ToLower();

        query = sortField switch
        {
            "name" => isAsc ? query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName) : query.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName),
            "employeecode" => isAsc ? query.OrderBy(e => e.EmployeeCode) : query.OrderByDescending(e => e.EmployeeCode),
            "email" => isAsc ? query.OrderBy(e => e.Email) : query.OrderByDescending(e => e.Email),
            "jobtitle" => isAsc ? query.OrderBy(e => e.JobRole != null ? e.JobRole.Title : string.Empty) : query.OrderByDescending(e => e.JobRole != null ? e.JobRole.Title : string.Empty),
            _ => isAsc ? query.OrderBy(e => e.JoinDate) : query.OrderByDescending(e => e.JoinDate)
        };

        // 6. Total Count before pagination in SQL
        var totalCount = await query.CountAsync(cancellationToken);

        // 7. Pagination bounds
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        // 8. Materialize only the requested page from SQL
        var pagedEmployees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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
