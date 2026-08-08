namespace Buy2.Domain.Entities;

public class RewardRedemption : BaseEntity
{
    public int RewardItemId { get; set; }
    public int EmployeeId { get; set; }
    public string VoucherCode { get; set; } = null!;
    public DateTimeOffset RedeemedAt { get; set; }
    public virtual Employee Employee { get; set; } = null!;
    public virtual RewardItem RewardItem { get; set; } = null!;
}
