namespace Buy2.Domain.Entities;

public class Reaction : BaseEntity
{
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
    public int? RecognitionId { get; set; }
    public string TargetType { get; set; } = "Post";
    public int TargetId { get; set; }
    public int EmployeeId { get; set; }
    public string ReactionType { get; set; } = string.Empty;

    public Post? Post { get; set; }
    public Comment? Comment { get; set; }
    public Recognition? Recognition { get; set; }
    public Employee? Employee { get; set; }
}
