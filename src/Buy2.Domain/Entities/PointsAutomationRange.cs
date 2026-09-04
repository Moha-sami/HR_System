using System.ComponentModel.DataAnnotations;

namespace Buy2.Domain.Entities;

public class PointsAutomationRange : BaseEntity
{
    public int AutomationSettingId { get; set; }
    public string RangeType { get; set; } = string.Empty;
    [Range(0, 100)]
    public decimal? FromValue { get; set; }
    [Range(0, 100)]
    public decimal? ToValue { get; set; }
    public string? TaskPriority { get; set; }
    public int PointsValue { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public PointsAutomationSetting AutomationSetting { get; set; } = null!;
}