namespace Buy2.Domain.Entities;

public class PerformanceMetric : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Target { get; set; }
    public decimal CurrentScore { get; set; }
    public string ScoreLabel { get; set; } = string.Empty;
    public decimal AllTimeAverage { get; set; }

    public ICollection<PerformanceSubmission> Submissions { get; set; } = new List<PerformanceSubmission>();
}
