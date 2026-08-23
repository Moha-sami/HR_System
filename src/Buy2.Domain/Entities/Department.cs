namespace Buy2.Domain.Entities;

public class Department : BaseEntity
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? HeadEmployeeId { get; set; }

    // Navigation Properties
    public Organization? Organization { get; set; }
    public Employee? HeadEmployee { get; set; }
}
