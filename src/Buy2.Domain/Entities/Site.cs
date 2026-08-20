namespace Buy2.Domain.Entities;

public class Site : BaseEntity
{
    public string SiteName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string MacAddressWhitelistJson { get; set; }= string.Empty;
    public string MacAddress { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string MapUrl { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }

    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public ICollection<SiteOperationalHour> OperationalHours { get; set; } = new List<SiteOperationalHour>();
    public ICollection<SitePreferredEmployee> PreferredEmployees { get; set; } = new List<SitePreferredEmployee>();
    public ICollection<SiteDocument> Documents { get; set; } = new List<SiteDocument>();
    public ICollection<EmployeeSite> EmployeeSites { get; set; } = new List<EmployeeSite>();
    public ICollection<ShiftEntity> Shifts { get; set; } = new List<ShiftEntity>();
}
