namespace Buy2.Domain.Entities;

public class PerformanceMetric : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Target { get; set; }
    public decimal Weight { get; set; }

    // Navigation Collection
    public ICollection<PerformanceSubmission> PerformanceSubmissions { get; set; } = new List<PerformanceSubmission>();
}
