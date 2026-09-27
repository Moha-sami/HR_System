using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.Requests;

public record SubmittedRequestSummaryDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string? EmployeeCode,
    string RequestType,
    string Category,
    DateTime SubmittedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    string? ManagerName,
    string ManagerStatus,
    string HrStatus,
    string Status,
    string? Reason,
    int AttachmentsCount
);

public record PaginatedListResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
)
{
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
