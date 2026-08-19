namespace Buy2.Domain.Entities;
public class SitePreferredEmployee {

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

}   