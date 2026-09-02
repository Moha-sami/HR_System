using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        IQueryable<JobRole> query = _jobRepository.Query(true)
            .Include(j => j.Department)
            .Include(j => j.Employees);

        // 1. Search Filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(search) ||
                                     (j.Department != null && j.Department.Name.ToLower().Contains(search)));
        }

        // 2. Department Filter
        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
        {
            query = query.Where(j => j.DepartmentId == request.DepartmentId.Value);
        }

        // 3. Seniority Level Filter
        if (!string.IsNullOrWhiteSpace(request.SeniorityLevel))
        {
            var seniority = request.SeniorityLevel.Trim();
            query = query.Where(j => j.SeniorityLevel == seniority);
        }

        // 4. Work Model Filter
        if (!string.IsNullOrWhiteSpace(request.WorkModel))
        {
            var workModel = request.WorkModel.Trim();
            query = query.Where(j => j.AttendanceType == workModel);
        }

        // 5. Active Status Filter
        if (request.IsActive.HasValue)
        {
            query = query.Where(j => j.IsActive == request.IsActive.Value);
        }

        // 6. Sorting
        var isDesc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var isAsc = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var sortField = request.SortBy?.Trim().ToLowerInvariant();

        query = sortField switch
        {
            "title" => isDesc ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "department" => isDesc ? query.OrderByDescending(j => j.Department != null ? j.Department.Name : string.Empty) : query.OrderBy(j => j.Department != null ? j.Department.Name : string.Empty),
            "createdat" => isAsc ? query.OrderBy(j => j.CreatedAt) : query.OrderByDescending(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.IsActive).ThenBy(j => j.Title)
        };

        var jobs = await query.ToListAsync(cancellationToken);

        // 7. Generate CSV with UTF-8 BOM
        var sb = new StringBuilder();
        sb.AppendLine("Job Title,Department,Seniority Level,Work Model,Experience (Years),Assigned Employees,Status,Created Date");

        foreach (var job in jobs)
        {
            var title = job.Title ?? "N/A";
            var dept = job.Department?.Name ?? "N/A";
            var seniority = job.SeniorityLevel ?? "N/A";
            var workModel = job.AttendanceType ?? "N/A";
            var exp = job.ExperienceYears;
            var assignedCount = job.Employees?.Count(e => !e.IsDeleted) ?? 0;
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
