using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.GetSubmittedRequests;

public class GetSubmittedRequestsQueryHandler : IRequestHandler<GetSubmittedRequestsQuery, PaginatedListResult<SubmittedRequestSummaryDto>>
{
    private readonly IRepository<Request> _requestRepository;

    public GetSubmittedRequestsQueryHandler(IRepository<Request> requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<PaginatedListResult<SubmittedRequestSummaryDto>> Handle(
        GetSubmittedRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _requestRepository.Query(asNoTracking: true)
            .Include(r => r.Employee)
            .Include(r => r.RequestType)
            .Include(r => r.Manager)
            .Include(r => r.Attachments)
            .AsQueryable();

        // 1. Text Search (Employee Name or ID)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var searchId))
            {
                query = query.Where(r => r.EmployeeId == searchId ||
                                         (r.Employee != null && (r.Employee.FirstName.Contains(search) || r.Employee.LastName.Contains(search))));
            }
            else
            {
                query = query.Where(r => r.Employee != null &&
                                         (r.Employee.FirstName.Contains(search) ||
                                          r.Employee.LastName.Contains(search) ||
                                          (r.Employee.FirstName + " " + r.Employee.LastName).Contains(search)));
            }
        }

        // 2. Date Range Filter
        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(r => r.SubmittedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(r => r.SubmittedAt < to);
        }

        // 3. Multi-Select Request Types Filter
        if (request.RequestTypeIds != null && request.RequestTypeIds.Any())
        {
            query = query.Where(r => request.RequestTypeIds.Contains(r.RequestTypeId));
        }

        // 4. Status Filters
        if (!string.IsNullOrWhiteSpace(request.ManagerStatus))
        {
            var managerStatus = request.ManagerStatus.Trim();
            query = query.Where(r => r.ManagerStatus == managerStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.HrStatus))
        {
            var hrStatus = request.HrStatus.Trim();
            query = query.Where(r => r.HrStatus == hrStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.OverallStatus))
        {
            var overallStatus = request.OverallStatus.Trim();
            query = query.Where(r => r.Status == overallStatus);
        }

        // 5. Dynamic Sorting
        var sortBy = (request.SortBy ?? "SubmittedAt").Trim().ToLowerInvariant();
        query = (sortBy, request.SortDescending) switch
        {
            ("employeename" or "employee", false) => query.OrderBy(r => r.Employee != null ? r.Employee.FirstName : string.Empty).ThenBy(r => r.SubmittedAt),
            ("employeename" or "employee", true) => query.OrderByDescending(r => r.Employee != null ? r.Employee.FirstName : string.Empty).ThenByDescending(r => r.SubmittedAt),
            ("requesttype", false) => query.OrderBy(r => r.RequestType != null ? r.RequestType.Name : string.Empty).ThenBy(r => r.SubmittedAt),
            ("requesttype", true) => query.OrderByDescending(r => r.RequestType != null ? r.RequestType.Name : string.Empty).ThenByDescending(r => r.SubmittedAt),
            ("status", false) => query.OrderBy(r => r.Status).ThenBy(r => r.SubmittedAt),
            ("status", true) => query.OrderByDescending(r => r.Status).ThenByDescending(r => r.SubmittedAt),
            (_, false) => query.OrderBy(r => r.SubmittedAt),
            _ => query.OrderByDescending(r => r.SubmittedAt)
        };

        // 6. Pagination
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new SubmittedRequestSummaryDto(
                r.Id,
                r.EmployeeId,
                r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}".Trim() : "Unknown",
                r.Employee != null ? r.Employee.NationalId : null,
                r.RequestType != null ? r.RequestType.Name : "Unknown",
                r.RequestType != null ? r.RequestType.Category : "General",
                r.SubmittedAt,
                r.StartDate,
                r.EndDate,
                r.Manager != null ? $"{r.Manager.FirstName} {r.Manager.LastName}".Trim() : null,
                r.ManagerStatus,
                r.HrStatus,
                r.Status,
                r.Reason,
                r.Attachments.Count
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedListResult<SubmittedRequestSummaryDto>(
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }
}
