using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class PayrollProfile : BaseEntity
{
    public decimal BaseSalary { get; set; }
    public SalaryType SalaryType { get; set; }
}
