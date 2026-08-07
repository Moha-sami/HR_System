namespace Buy2.Domain.Entities;
public class RewardItem : BaseEntity
{
    public string RewardName { get; set; } = null!;

    public int CostInPoints { get; set; }

    public int AvailableStock { get; set; }
}