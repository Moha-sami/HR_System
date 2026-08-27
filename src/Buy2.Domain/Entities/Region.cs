namespace Buy2.Domain.Entities;
public class Region : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Site> Sites { get; set; } = new List<Site>();
}