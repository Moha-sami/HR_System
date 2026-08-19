namespace Buy2.Domain.Entities;
public class SiteOperationalHour : BaseEntity
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeOnly OpenTime {  get; set; }
    public TimeOnly CloseTime { get; set; }
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
}
