using System.ComponentModel.DataAnnotations;

namespace Buy2.Domain.Entities;

public class PerformanceSubmission : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int MetricId { get; set; }
    public PerformanceMetric? PerformanceMetric { get; set; }
    
    [Range(0, 100)]
    public decimal AchievedPercent { get; set; }
    
    public decimal Score { get; set; }
    
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    
    public string Feedback { get; set; } = string.Empty;
}