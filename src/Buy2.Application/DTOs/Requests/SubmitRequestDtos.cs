using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.Requests;

public record RequestAttachmentDto(
    int Id,
    string FileName,
    string StorageUrl,
    string? ContentType,
    long FileSize,
    DateTime UploadedAt
);

public record SubmittedRequestResponseDto(
    int Id,
    int EmployeeId,
    int RequestTypeId,
    string RequestTypeName,
    string Category,
    string Status,
    string ManagerStatus,
    string HrStatus,
    int? ManagerId,
    string? ManagerName,
    DateTime SubmittedAt,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Reason,
    string? CategoryValuesJson,
    IReadOnlyList<RequestAttachmentDto> Attachments
);

public class SubmitRequestModel
{
    public int? EmployeeId { get; set; }
    public int RequestTypeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Reason { get; set; }
    public string? CategoryValuesJson { get; set; }
}
