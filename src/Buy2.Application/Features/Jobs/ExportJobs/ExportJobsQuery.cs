using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Jobs.ExportJobs;

public record ExportJobsQuery(
    string? SearchTerm = null,
    int? DepartmentId = null,
    string? SeniorityLevel = null,
    string? WorkModel = null,
    bool? IsActive = null,
    string? SortBy = null,
    string? SortDir = "desc"
) : IRequest<byte[]>;

public class ExportJobsQueryHandler : IRequestHandler<ExportJobsQuery, byte[]>
{
    private readonly IRepository<JobRole> _jobRepository;

    public ExportJobsQueryHandler(IRepository<JobRole> jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<byte[]> Handle(ExportJobsQuery request, CancellationToken cancellationToken)
    {
        var spec = new Specification<JobRole>()
            .Include(nameof(JobRole.Department), nameof(JobRole.Employees));

        // 1. Search Filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            spec.Where(j => j.Title.ToLower().Contains(search) ||
                            (j.Department != null && j.Department.Name.ToLower().Contains(search)));
        }

        // 2. Department Filter
        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
        {
            var departmentId = request.DepartmentId.Value;
            spec.Where(j => j.DepartmentId == departmentId);
        }

        // 3. Seniority Level Filter
        if (!string.IsNullOrWhiteSpace(request.SeniorityLevel))
        {
            var seniority = request.SeniorityLevel.Trim();
            spec.Where(j => j.SeniorityLevel == seniority);
        }

        // 4. Work Model Filter
        if (!string.IsNullOrWhiteSpace(request.WorkModel))
        {
            var workModel = request.WorkModel.Trim();
            spec.Where(j => j.AttendanceType == workModel);
        }

        // 5. Active Status Filter
        if (request.IsActive.HasValue)
        {
            var isActive = request.IsActive.Value;
            spec.Where(j => j.IsActive == isActive);
        }

        // 6. Sorting
        var isDesc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var isAsc = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var sortField = request.SortBy?.Trim().ToLowerInvariant();

        switch (sortField)
        {
            case "title":
                spec.OrderBy(j => j.Title, descending: isDesc);
                break;
            case "department":
                spec.OrderBy(j => j.Department != null ? j.Department.Name : string.Empty, descending: isDesc);
                break;
            case "createdat":
                spec.OrderBy(j => j.CreatedAt, descending: !isAsc);
                break;
            default:
                spec.OrderBy(j => j.IsActive, descending: true).ThenBy(j => j.Title);
                break;
        }

        var entities = await _jobRepository.ListAsync(spec, cancellationToken);

        var jobs = entities
            .Select(j => new
            {
                Title = j.Title,
                DepartmentName = j.Department != null ? j.Department.Name : "N/A",
                SeniorityLevel = j.SeniorityLevel,
                WorkModel = j.AttendanceType,
                ExperienceYears = j.ExperienceYears,
                AssignedEmployeesCount = j.Employees != null ? j.Employees.Count(e => !e.IsDeleted) : 0,
                IsActive = j.IsActive,
                CreatedAt = j.CreatedAt
            })
            .ToList();

        // 7. Generate CSV with UTF-8 BOM
        var sb = new StringBuilder();
        sb.AppendLine("Job Title,Department,Seniority Level,Work Model,Experience (Years),Assigned Employees,Status,Created Date");

        foreach (var job in jobs)
        {
            var title = job.Title ?? "N/A";
            var dept = job.DepartmentName ?? "N/A";
            var seniority = job.SeniorityLevel ?? "N/A";
            var workModel = job.WorkModel ?? "N/A";
            var exp = job.ExperienceYears;
            var assignedCount = job.AssignedEmployeesCount;
            var status = job.IsActive ? "Active" : "Inactive";
            var createdDate = job.CreatedAt.ToString("yyyy-MM-dd");

            sb.AppendLine($"{EscapeCsv(title)},{EscapeCsv(dept)},{EscapeCsv(seniority)},{EscapeCsv(workModel)},{exp},{assignedCount},{status},{createdDate}");
        }

        var bom = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + contentBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, bom.Length, contentBytes.Length);

        return result;
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
