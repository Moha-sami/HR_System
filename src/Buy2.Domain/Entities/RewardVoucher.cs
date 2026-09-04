using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;
public class RewardVoucher : BaseEntity
{
    public int BatchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public VoucherStatus Status { get; set; }
    public int RewardItemId { get; set; }
    public RewardItem RewardItem { get; set; } = null!;
}