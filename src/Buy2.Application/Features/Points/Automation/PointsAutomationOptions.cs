namespace Buy2.Application.Features.Points.Automation;

public class PointsAutomationOptions
{
    public const string SectionName = "PointsAutomation";

    public bool Enabled { get; set; } = true;
    public string TimeZone { get; set; } = "Africa/Cairo";
    public string DailyDispatcherCron { get; set; } = "5 0 * * *";
    public int MaxCatchUpWindowsPerPeriod { get; set; } = 10;
}
