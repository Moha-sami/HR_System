using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Buy2.Application.Features.Jobs.GetJobs;

public record GetJobsQuery(JobFilterQueryDto Filter) : IRequest<JobPaginatedResponseDto<JobListItemDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, JobPaginatedResponseDto<JobListItemDto>>
{
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetJobsQueryHandler(IRepository<JobRole> jobRoleRepository, IRepository<Employee> employeeRepository)
    {
        _jobRoleRepository = jobRoleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<JobPaginatedResponseDto<JobListItemDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new JobFilterQueryDto();
        var page = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        // NOTE: Employees are intentionally NOT included here. Loading the whole
        // collection per job just to count it is the over-fetching anti-pattern
        // (perf. test Issue #316). Counts come from a second filtered query below.
        var spec = new Specification<JobRole>()
            .Include(nameof(JobRole.Department));

        ApplyFilters(spec, filter);
        ApplySorting(spec, filter.SortBy, filter.SortDir);

        var paged = await _jobRoleRepository.PagedAsync(spec, page, pageSize, cancellationToken);

        var countByJobRoleId = await GetActiveEmployeeCountsAsync(
            paged.Items.Select(j => j.Id).ToList(), cancellationToken);

        var items = paged.Items.Select(j => new JobListItemDto(
            j.Id,
            j.Title,
            j.DepartmentId,
            j.Department != null ? j.Department.Name : "N/A",
            j.SeniorityLevel,
            j.AttendanceType,
            countByJobRoleId.TryGetValue(j.Id, out var employeeCount) ? employeeCount : 0,
            ParseJsonListCount(j.RequiredQualificationsJson),
            j.ExperienceYears,
            j.IsActive,
            j.CreatedAt
        )).ToList();

        return new JobPaginatedResponseDto<JobListItemDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize, paged.TotalPages);
    }

    private static Specification<JobRole> ApplyFilters(Specification<JobRole> spec, JobFilterQueryDto filter)
    {
        ApplySearchTerm(spec, filter.SearchTerm);
        ApplyDepartmentAndStatusFilter(spec, filter.DepartmentId, filter.IsActive);
        ApplyWorkModelAndSeniorityFilter(spec, filter.WorkModel, filter.SeniorityLevel);
        return spec;
    }

    private static Specification<JobRole> ApplySearchTerm(Specification<JobRole> spec, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return spec;
        }

        var term = searchTerm.Trim().ToLower();
        spec.Where(j => j.Title.ToLower().Contains(term) ||
                        (j.Department != null && j.Department.Name.ToLower().Contains(term)));
        return spec;
    }

    private static Specification<JobRole> ApplyDepartmentAndStatusFilter(Specification<JobRole> spec, int? departmentId, bool? isActive)
    {
        if (departmentId.HasValue)
        {
            var id = departmentId.Value;
            spec.Where(j => j.DepartmentId == id);
        }

        if (isActive.HasValue)
        {
            var active = isActive.Value;
            spec.Where(j => j.IsActive == active);
        }

        return spec;
    }

    private static Specification<JobRole> ApplyWorkModelAndSeniorityFilter(Specification<JobRole> spec, string? workModel, string? seniorityLevel)
    {
        if (!string.IsNullOrWhiteSpace(seniorityLevel))
        {
            spec.Where(j => j.SeniorityLevel == seniorityLevel);
        }

        if (!string.IsNullOrWhiteSpace(workModel))
        {
            spec.Where(j => j.AttendanceType == workModel);
        }

        return spec;
    }

    private static Specification<JobRole> ApplySorting(Specification<JobRole> spec, string? sortBy, string? sortDir)
    {
        var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var isAsc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        switch (sortBy?.ToLowerInvariant())
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

        return spec;
    }


    private async Task<Dictionary<int, int>> GetActiveEmployeeCountsAsync(
        List<int> jobRoleIds, CancellationToken cancellationToken)
    {
        if (jobRoleIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var roleIds = await _employeeRepository.ListAsync(
            new Specification<Employee>()
                .Where(e => jobRoleIds.Contains(e.JobRoleId) && !e.IsDeleted),
            e => e.JobRoleId,
            cancellationToken);

        return roleIds
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static int ParseJsonListCount(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
