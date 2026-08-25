using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class Department : BaseEntity
{
    public int OrganizationId { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? HeadEmployeeId { get; set; }
    public Employee? HeadEmployee { get; set; }
    public bool IsActive { get; set; } = true;
    public new DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public Organization? Organization { get; set; }
    public ICollection<JobRole> JobRoles { get; set; } = new List<JobRole>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
