using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class DisciplinaryViolation : BaseEntity
{
    public int EmployeeId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ViolationType ViolationType { get; set; } = ViolationType.Attendance;
    public int? ReportedById { get; set; }
    public string? WitnessesJson { get; set; }
    public string? DocumentUrl { get; set; }
    public ViolationStatus Status { get; set; } = ViolationStatus.Pending;
    public string? ActionType { get; set; }
    public DateTime? ActionDate { get; set; }
    public int? ActionTakenById { get; set; }
    public string? ActionDescription { get; set; }

    // Navigation Properties
    public Employee? Employee { get; set; }
    public Employee? ReportedBy { get; set; }
    public Employee? ActionTakenBy { get; set; }
}
