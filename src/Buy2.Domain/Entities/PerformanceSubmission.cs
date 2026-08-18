namespace Buy2.Domain.Entities;

public class PerformanceSubmission : BaseEntity
{
    public int EmployeeId { get; set; }
    public int MetricId { get; set; }
    public decimal ActualValue { get; set; }
    public string Comments { get; set; } = string.Empty;

    // Navigation Properties
    public Employee? Employee { get; set; }
    public PerformanceMetric? Metric { get; set; }
}
