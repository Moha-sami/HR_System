using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.Requests;

public record PreviousRequestSummaryDto(
    int Id,
    string RequestType,
    string Category,
    DateTime SubmittedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    DateTime? ResolvedAt
);

public record RequestReviewNoteDto(
    string ReviewerRole,
    int? ReviewerId,
    string? ReviewerName,
    string Status,
    string? Comment,
    DateTime? DecisionAt
);

public record RequestDetailDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string? EmployeeCode,
    string? DepartmentName,
    string RequestType,
    string Category,
    DateTime SubmittedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Reason,
    string? CategoryValuesJson,
    string OverallStatus,
    string? RejectionReason,
    RequestReviewNoteDto ManagerReview,
    RequestReviewNoteDto HrReview,
    IReadOnlyList<RequestAttachmentDto> Attachments,
    IReadOnlyList<PreviousRequestSummaryDto> PreviousRequests
);
