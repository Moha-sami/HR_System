using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.GetRequestsHistory;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Requests.ExportRequestsHistory;

public class ExportRequestsHistoryQueryHandler : IRequestHandler<ExportRequestsHistoryQuery, ExportRequestsHistoryResult>
{
    private readonly IRepository<Request> _requestRepository;

    public ExportRequestsHistoryQueryHandler(IRepository<Request> requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<ExportRequestsHistoryResult> Handle(
        ExportRequestsHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = _requestRepository.Query(asNoTracking: true)
            .Include(r => r.Employee)
            .Include(r => r.RequestType)
            .Include(r => r.Manager)
            .Include(r => r.Attachments)
            .AsQueryable();

        // 1. Audit ledger queries historical resolved requests (Approved or Rejected)
        if (!string.IsNullOrWhiteSpace(request.OverallStatus))
        {
            var overallStatus = request.OverallStatus.Trim();
            query = query.Where(r => r.Status == overallStatus);
        }
        else
        {
            query = query.Where(r => r.Status == "Approved" || r.Status == "Rejected");
        }

        // 2. Text Search (Employee Name, Employee Code, or Request Type)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var searchId))
            {
                query = query.Where(r => r.EmployeeId == searchId ||
                                         (r.Employee != null && (r.Employee.FirstName.Contains(search) || r.Employee.LastName.Contains(search))) ||
                                         (r.RequestType != null && r.RequestType.Name.Contains(search)));
            }
            else
            {
                query = query.Where(r => (r.Employee != null && (r.Employee.FirstName.Contains(search) ||
                                                                 r.Employee.LastName.Contains(search) ||
                                                                 (r.Employee.FirstName + " " + r.Employee.LastName).Contains(search) ||
                                                                 (r.Employee.NationalId != null && r.Employee.NationalId.Contains(search)))) ||
                                         (r.RequestType != null && r.RequestType.Name.Contains(search)));
            }
        }

        // 3. Date Range Filter
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

        // 4. Multi-Select Request Types Filter
        if (request.RequestTypeIds != null && request.RequestTypeIds.Any())
        {
            query = query.Where(r => request.RequestTypeIds.Contains(r.RequestTypeId));
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
            ("resolvedat", false) => query.OrderBy(r => r.ResolvedAt ?? r.SubmittedAt).ThenBy(r => r.SubmittedAt),
            ("resolvedat", true) => query.OrderByDescending(r => r.ResolvedAt ?? r.SubmittedAt).ThenByDescending(r => r.SubmittedAt),
            ("submittedat" or "submissiondate", false) => query.OrderBy(r => r.SubmittedAt),
            ("submittedat" or "submissiondate", true) => query.OrderByDescending(r => r.SubmittedAt),
            (_, false) => query.OrderBy(r => r.SubmittedAt),
            _ => query.OrderByDescending(r => r.SubmittedAt)
        };

        var items = await query
            .Select(r => new RequestHistorySummaryDto(
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
                r.ManagerComment,
                r.HrStatus,
                r.HrComment,
                r.Status,
                r.RejectionReason,
                r.ResolvedAt ?? r.HrDecisionAt ?? r.ManagerDecisionAt ?? r.SubmittedAt,
                r.Attachments.Count
            ))
            .ToListAsync(cancellationToken);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var isExcel = string.Equals(request.Format?.Trim(), "xlsx", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(request.Format?.Trim(), "excel", StringComparison.OrdinalIgnoreCase);

        if (isExcel)
        {
            var content = RequestHistoryExportBuilder.BuildExcel(items);
            return new ExportRequestsHistoryResult(
                Content: content,
                ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName: $"requests_history_{timestamp}.xlsx"
            );
        }
        else
        {
            var content = RequestHistoryExportBuilder.BuildCsv(items);
            return new ExportRequestsHistoryResult(
                Content: content,
                ContentType: "text/csv; charset=utf-8",
                FileName: $"requests_history_{timestamp}.csv"
            );
        }
    }
}
