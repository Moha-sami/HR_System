using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class PlannedBlock
{
    public ShiftEntity Entity { get; set; } = null!;
    public string RoleTitle { get; set; } = string.Empty;
    public int? SourceEmployeeId { get; set; }
    public string? SourceEmployeeName { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public bool Stripped { get; set; }
    public string? StripCode { get; set; }
    public string? StripReason { get; set; }
    public bool Collision { get; set; }
    public string? CollisionType { get; set; }
    public bool Conflict { get; set; }
    public string? ConflictType { get; set; }
    public bool Pruned { get; set; }
    public HashSet<int> OverlappingExistingIds { get; } = new();
}

public sealed record StripDecision(string Code, string Reason);
