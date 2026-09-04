using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class PointsAutomationSetting : BaseEntity
{
    public AutomationCategory Category { get; set; }
    public string SubCategory { get; set; } = string.Empty;
    public AutomationPeriod AutomationPeriod { get; set; } = AutomationPeriod.Monthly;
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    public int? MetricId { get; set; }

    public ICollection<PointsAutomationRange> Ranges { get; set; } = new List<PointsAutomationRange>();
    public PerformanceMetric? Metric { get; set; }
}