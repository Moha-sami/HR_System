using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class PayrollProfile : BaseEntity
{
    public int EmployeeId { get; set; }
    public SalaryType SalaryType { get; set; }
    public string PayoutPeriod { get; set; } = string.Empty;
    public int PayoutDay { get; set; }
    public DayOfWeek WorkWeekStart { get; set; } = DayOfWeek.Sunday;
    public DayOfWeek WorkWeekEnd { get; set; } = DayOfWeek.Thursday;
    public decimal PaymentAmount { get; set; }
    public decimal OvertimeThresholdHours { get; set; }
    public decimal OvertimeHourlyRate { get; set; }

    // Navigation Property (1:1 with Employee)
    public Employee? Employee { get; set; }
}
