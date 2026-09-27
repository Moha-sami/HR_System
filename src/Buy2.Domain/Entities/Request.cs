using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class Request : BaseEntity
{
    public int EmployeeId { get; set; }
    public int RequestTypeId { get; set; }
    public int? ApproverId { get; set; }
    public int? ManagerId { get; set; }
    public int? HrId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public string ManagerStatus { get; set; } = "Pending";
    public string? ManagerComment { get; set; }
    public DateTime? ManagerDecisionAt { get; set; }
    public string HrStatus { get; set; } = "Pending";
    public string? HrComment { get; set; }
    public DateTime? HrDecisionAt { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? CategoryValuesJson { get; set; }

    public Employee? Employee { get; set; }
    public RequestType? RequestType { get; set; }
    public Employee? Approver { get; set; }
    public Employee? Manager { get; set; }
    public Employee? Hr { get; set; }
    public ICollection<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
}

