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

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(term) ||
                                     (j.Department != null && j.Department.Name.ToLower().Contains(term)));
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(j => j.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SeniorityLevel))
        {
            query = query.Where(j => j.SeniorityLevel == filter.SeniorityLevel);
        }

        if (!string.IsNullOrWhiteSpace(filter.WorkModel))
        {
            query = query.Where(j => j.AttendanceType == filter.WorkModel);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(j => j.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Dynamic sorting
        query = filter.SortBy?.ToLower() switch
        {
            "title" => filter.SortDir?.ToLower() == "desc" ? query.OrderByDescending(j => j.Title) : query.OrderBy(j => j.Title),
            "department" => filter.SortDir?.ToLower() == "desc" ? query.OrderByDescending(j => j.Department != null ? j.Department.Name : string.Empty) : query.OrderBy(j => j.Department != null ? j.Department.Name : string.Empty),
            "createdat" => filter.SortDir?.ToLower() == "asc" ? query.OrderBy(j => j.CreatedAt) : query.OrderByDescending(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.IsActive).ThenBy(j => j.Title)
        };

        var rawList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawList.Select(j => new JobListItemDto(
            j.Id,
            j.Title,
            j.DepartmentId,
            j.Department?.Name ?? "N/A",
            j.SeniorityLevel,
            j.AttendanceType,
            j.Employees?.Count(e => !e.IsDeleted) ?? 0,
            ParseJsonListCount(j.RequiredQualificationsJson),
            j.ExperienceYears,
            j.IsActive,
            j.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new JobPaginatedResponseDto<JobListItemDto>(items, totalCount, page, pageSize, totalPages);
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
