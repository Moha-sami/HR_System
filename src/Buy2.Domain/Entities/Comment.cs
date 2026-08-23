namespace Buy2.Domain.Entities;

public class Comment : BaseEntity
{
    public int PostId { get; set; }
    public int AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;

    public Post? Post { get; set; }
    public Employee? Author { get; set; }
}
