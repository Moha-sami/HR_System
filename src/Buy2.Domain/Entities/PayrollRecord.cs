namespace Buy2.Domain.Entities;

public class PayrollRecord : BaseEntity
{
    public int EmployeeId { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Bonuses { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PayDate { get; set; }
    public string? PayslipUrl { get; set; }

    public Employee? Employee { get; set; }
}
