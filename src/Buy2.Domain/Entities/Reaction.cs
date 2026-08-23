namespace Buy2.Domain.Entities;

public class Reaction : BaseEntity
{
    public int PostId { get; set; }
    public int EmployeeId { get; set; }
    public string ReactionType { get; set; } = string.Empty;

    public Post? Post { get; set; }
    public Employee? Employee { get; set; }
}
