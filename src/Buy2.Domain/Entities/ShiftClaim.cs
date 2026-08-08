namespace Buy2.Domain.Entities;

public class ShiftClaim : BaseEntity
{
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OvertimeJustification { get; set; } = string.Empty;
}