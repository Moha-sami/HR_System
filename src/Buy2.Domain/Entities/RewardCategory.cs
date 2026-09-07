namespace Buy2.Domain.Entities;

public class RewardCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<RewardItem> RewardItems { get; set; } = new List<RewardItem>();
}