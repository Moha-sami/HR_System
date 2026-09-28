using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class Post : BaseEntity
{
    public int AuthorId { get; set; }
    public int? OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Status { get; set; } = "Draft";
    public DateTime? ScheduledFor { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? MediaUrl { get; set; }
    public string PostType { get; set; } = "News";
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime? UpdatedAt { get; set; }

    public Employee? Author { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}
