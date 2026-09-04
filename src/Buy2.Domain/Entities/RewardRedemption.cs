namespace Buy2.Domain.Entities;

public class RewardRedemption : BaseEntity
{
    public int RewardItemId { get; set; }
    public int EmployeeId { get; set; }
    public string VoucherCode { get; set; } = null!;
    public int RewardVoucherId { get; set; }
    public DateTimeOffset RedeemedAt { get; set; }
    public Employee Employee { get; set; } = null!;
    public RewardItem RewardItem { get; set; } = null!;
    public RewardVoucher RewardVoucher { get; set; } = null!;
    public int PointTransactionId { get; set; }
    public PointsTransaction PointsTransaction { get; set; } = null!;
}
