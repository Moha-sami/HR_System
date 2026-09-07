namespace Buy2.Domain.Entities;

public class RewardItem : BaseEntity
{
    public string RewardName { get; set; } = null!;
    public int CostInPoints { get; set; }
    public int AvailableStock { get; set; }

    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public RewardCategory Category { get; set; } = null!;
    public decimal MonetaryValue { get; set; }
    public string? BannerImageUrl { get; set; }
    public string HowToRedeem { get; set; } = string.Empty;
    public string TermsOfUse { get; set; } = string.Empty;
    public ICollection<RewardVoucher> Vouchers { get; set; }
        = new List<RewardVoucher>();
    public ICollection<RewardRedemption> Redemptions { get; set; }
        = new List<RewardRedemption>();
    public bool IsActive { get; set; }
}