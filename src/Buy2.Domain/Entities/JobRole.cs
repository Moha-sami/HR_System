using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class JobRole : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Description { get; set; }
    public string SeniorityLevel { get; set; } = "Junior";
    public string RequiredQualificationsJson { get; set; } = "[]";
    public int ExperienceYears { get; set; } = 0;
    public string AttendanceType { get; set; } = "OnSite";
    public string OnlineWorkdaysJson { get; set; } = "[]";
    public string OfflineWorkdaysJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }
    public new DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
