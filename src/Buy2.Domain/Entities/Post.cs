namespace Buy2.Domain.Entities;

public class Post : BaseEntity
{
    public int AuthorId { get; set; }
    public int? OrganizationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string PostType { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }

    public Employee? Author { get; set; }
}
