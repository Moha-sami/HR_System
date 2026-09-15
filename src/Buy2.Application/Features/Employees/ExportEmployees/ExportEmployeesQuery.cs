using System.Text;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Employees.ExportEmployees;

public record ExportEmployeesQuery(
    string? Department = null,
    string? Region = null,
    string? Search = null,
    string? Sort = null,
    string? SortDir = "desc"
) : IRequest<byte[]>;

public class ExportEmployeesQueryHandler : IRequestHandler<ExportEmployeesQuery, byte[]>
{
    private readonly IRepository<Employee> _employeeRepository;

    public ExportEmployeesQueryHandler(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<byte[]> Handle(ExportEmployeesQuery request, CancellationToken cancellationToken)
    {
        // 1. Specification with Eager Loaded Navigations
        var spec = new Specification<Employee>()
            .Include(nameof(Employee.JobRole))
            .Include(nameof(Employee.Site))
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
                spec.Where(e => e.JobRole != null && e.JobRole.Title.Contains(department));
            }
        }

        // 4. Region Filter (translated to SQL)
        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var region = request.Region.Trim();
            spec.Where(e => e.Site != null && e.Site.SiteName.Contains(region));
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

        // 6. Fetch all matching employees without pagination
        var employees = await _employeeRepository.ListAsync(spec, cancellationToken);

        // 7. Generate CSV string
        var sb = new StringBuilder();
        sb.AppendLine("Employee Code,First Name,Last Name,Email,Phone,Job Title,Site,Join Date,Admin Access");

        foreach (var emp in employees)
        {
            var code = string.IsNullOrEmpty(emp.EmployeeCode) ? $"EMP-{emp.Id:D4}" : emp.EmployeeCode;
            var jobTitle = emp.JobRole?.Title ?? "N/A";
            var siteName = emp.Site?.SiteName ?? "N/A";
            var isAdmin = emp.Role != null && (
                emp.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) || 
                emp.Role.Name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

            sb.AppendLine($"{EscapeCsv(code)},{EscapeCsv(emp.FirstName)},{EscapeCsv(emp.LastName)},{EscapeCsv(emp.Email)},{EscapeCsv(emp.PhoneNumber)},{EscapeCsv(jobTitle)},{EscapeCsv(siteName)},{emp.JoinDate:yyyy-MM-dd},{isAdmin}");
        }

        // 8. Return UTF-8 bytes with BOM for Excel compatibility
        var bom = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + contentBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);

        return result;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
