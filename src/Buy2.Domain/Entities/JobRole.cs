namespace Buy2.Domain.Entities;

public class JobRole : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string RequiredQualificationsJson { get; set; } = string.Empty;

    // Navigation Property
    public Department? Department { get; set; }
}
