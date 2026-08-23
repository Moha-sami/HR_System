namespace Buy2.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;

    public ICollection<Department> Departments { get; set; } = new List<Department>();
}
