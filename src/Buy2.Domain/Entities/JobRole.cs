using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class JobRole : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DepartmentId { get; set; }
    public string SeniorityLevel { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string AttendanceType { get; set; } = string.Empty;
    public string? OnlineWorkdaysJson { get; set; }
    public string? OfflineWorkdaysJson { get; set; }
    public string RequiredQualificationsJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public Department? Department { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
