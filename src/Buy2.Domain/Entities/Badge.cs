namespace Buy2.Domain.Entities;

public class Badge : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public decimal TriggerThreshold { get; set; }
}
