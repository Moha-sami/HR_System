namespace Buy2.Domain.Entities;

public class Site : BaseEntity
{
    public string SiteName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string MacAddressWhitelistJson { get; set; } = string.Empty;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
