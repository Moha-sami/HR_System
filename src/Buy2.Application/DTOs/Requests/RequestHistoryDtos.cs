using System;

namespace Buy2.Application.DTOs.Requests;

public record RequestHistorySummaryDto(
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
    string? ManagerComment,
    string HrStatus,
    string? HrComment,
    string Status,
    string? RejectionReason,
    DateTime? ResolvedAt,
    int AttachmentsCount
);

public record ExportRequestsHistoryResult(
    byte[] Content,
    string ContentType,
    string FileName
);
