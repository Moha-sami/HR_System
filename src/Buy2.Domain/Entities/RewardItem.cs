namespace Buy2.Domain.Entities;
public class RewardItem : BaseEntity
{
    public string RewardName { get; set; }

    public int CostInPoints { get; set; }

    public int AvailableStock { get; set; }
}