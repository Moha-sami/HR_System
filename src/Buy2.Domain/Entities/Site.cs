namespace Buy2.Domain.Entities;

public class Site : BaseEntity
{
    public string SiteName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string MacAddressWhitelistJson { get; set; }= string.Empty;
    public ICollection<EmployeeSite> EmployeeSites { get; set; } = new List<EmployeeSite>();
}
