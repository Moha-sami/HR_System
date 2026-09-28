using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class Comment : BaseEntity
{
    public int PostId { get; set; }
    public int AuthorId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public bool IsModerated { get; set; } = false;
    public string? ModerationReason { get; set; }

    public Post? Post { get; set; }
    public Employee? Author { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}
