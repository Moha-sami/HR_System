using System;

namespace Buy2.Application.DTOs.Requests;

public class ProcessDecisionModel
{
    public string Tier { get; set; } = "Manager";
    public string Decision { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? RejectionReason { get; set; }
    public int? ReviewerId { get; set; }
}

public record ProcessDecisionResponseDto(
    int RequestId,
    string OverallStatus,
    string ManagerStatus,
    string? ManagerComment,
    DateTime? ManagerDecisionAt,
    string HrStatus,
    string? HrComment,
    DateTime? HrDecisionAt,
    string? RejectionReason,
    DateTime? ResolvedAt
);
