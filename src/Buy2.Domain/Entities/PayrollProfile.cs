namespace Buy2.Domain.Entities;

public class PayrollProfile : BaseEntity
{
    public int EmployeeId { get; set; }
    public string SalaryType { get; set; } = string.Empty;
    public string PayoutPeriod { get; set; } = string.Empty;
    public int PayoutDay { get; set; }
    public string WorkWeekStart { get; set; } = string.Empty;
    public string WorkWeekEnd { get; set; } = string.Empty;
    public decimal PaymentAmount { get; set; }
    public decimal OvertimeThresholdHours { get; set; }
    public decimal OvertimeHourlyRate { get; set; }
    public string AttendanceType { get; set; } = string.Empty;
    public string? WorkSiteIdsJson { get; set; }
    public string? OnlineWorkdaysJson { get; set; }
    public string? OfflineWorkdaysJson { get; set; }

    public Employee? Employee { get; set; }
}
