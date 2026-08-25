using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var page = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        IQueryable<JobRole> query = _jobRoleRepository.Query(asNoTracking: true)
            .Include(j => j.Department)
            .Include(j => j.Employees);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim();
            query = query.Where(j => j.Title.Contains(searchTerm) || (j.Department != null && j.Department.Name.Contains(searchTerm)));
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

        query = query.OrderByDescending(j => j.IsActive).ThenBy(j => j.Title);

        var rawList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rawList.Select(j => new JobListItemDto(
            j.Id,
            j.Title,
            j.DepartmentId,
            j.Department != null ? j.Department.Name : "N/A",
            j.SeniorityLevel,
            j.AttendanceType,
            j.Employees != null ? j.Employees.Count(e => !e.IsDeleted) : 0,
            ParseJsonListCount(j.RequiredQualificationsJson),
            j.ExperienceYears,
            j.IsActive,
            j.CreatedAt
        )).ToList();

        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

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
