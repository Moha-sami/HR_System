namespace Buy2.Domain.Entities;

public class EmployeeSite
{
    public int EmployeeId { get; set; }
    public int SiteId { get; set; }

    // Navigation Properties
    public Employee Employee { get; set; } = null!;
    public Site Site { get; set; } = null!;
}
