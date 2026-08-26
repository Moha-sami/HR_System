using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Buy2.Application.Features.Jobs.GetJobs;

public record GetJobsQuery(JobFilterQueryDto Filter) : IRequest<JobPaginatedResponseDto<JobListItemDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, JobPaginatedResponseDto<JobListItemDto>>
{
    private readonly IRepository<JobRole> _jobRoleRepository;

    public GetJobsQueryHandler(IRepository<JobRole> jobRoleRepository)
    {
        _jobRoleRepository = jobRoleRepository;
    }

    public async Task<JobPaginatedResponseDto<JobListItemDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new JobFilterQueryDto();
        var page = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        IQueryable<JobRole> query = _jobRoleRepository.Query(asNoTracking: true)
            .Include(j => j.Department)
            .Include(j => j.Employees);

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, filter.SortBy, filter.SortDir);

        var rawList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawList.Select(MapToListItemDto).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new JobPaginatedResponseDto<JobListItemDto>(items, totalCount, page, pageSize, totalPages);
    }

    private static IQueryable<JobRole> ApplyFilters(IQueryable<JobRole> query, JobFilterQueryDto filter)
    {
        query = ApplySearchTerm(query, filter.SearchTerm);
        query = ApplyDepartmentAndStatusFilter(query, filter.DepartmentId, filter.IsActive);
        query = ApplyWorkModelAndSeniorityFilter(query, filter.WorkModel, filter.SeniorityLevel);
        return query;
    }

    private static IQueryable<JobRole> ApplySearchTerm(IQueryable<JobRole> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var term = searchTerm.Trim().ToLower();
        return query.Where(j => j.Title.ToLower().Contains(term) ||
                                 (j.Department != null && j.Department.Name.ToLower().Contains(term)));
    }

    private static IQueryable<JobRole> ApplyDepartmentAndStatusFilter(IQueryable<JobRole> query, int? departmentId, bool? isActive)
    {
        if (departmentId.HasValue)
        {
            query = query.Where(j => j.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(j => j.IsActive == isActive.Value);
        }

        return query;
    }

    private static IQueryable<JobRole> ApplyWorkModelAndSeniorityFilter(IQueryable<JobRole> query, string? workModel, string? seniorityLevel)
    {
        if (!string.IsNullOrWhiteSpace(seniorityLevel))
        {
            query = query.Where(j => j.SeniorityLevel == seniorityLevel);
        }

        if (!string.IsNullOrWhiteSpace(workModel))
        {
            query = query.Where(j => j.AttendanceType == workModel);
        }

        return query;
    }

    private static IQueryable<JobRole> ApplySorting(IQueryable<JobRole> query, string? sortBy, string? sortDir)
    {
        var isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var isAsc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLowerInvariant() switch
        {
            "title" => isDesc ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "department" => isDesc ? query.OrderByDescending(j => j.Department != null ? j.Department.Name : string.Empty) : query.OrderBy(j => j.Department != null ? j.Department.Name : string.Empty),
            "createdat" => isAsc ? query.OrderBy(j => j.CreatedAt) : query.OrderByDescending(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.IsActive).ThenBy(j => j.Title)
        };
    }

    private static JobListItemDto MapToListItemDto(JobRole j)
    {
        var employeeCount = j.Employees?.Count(e => !e.IsDeleted) ?? 0;
        var departmentName = j.Department?.Name ?? "N/A";
        var qualCount = ParseJsonListCount(j.RequiredQualificationsJson);

        return new JobListItemDto(
            j.Id,
            j.Title,
            j.DepartmentId,
            departmentName,
            j.SeniorityLevel,
            j.AttendanceType,
            employeeCount,
            qualCount,
            j.ExperienceYears,
            j.IsActive,
            j.CreatedAt
        );
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
