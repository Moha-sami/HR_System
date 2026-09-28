using System;
using System.Collections.Generic;

namespace Buy2.Domain.Entities;

public class Recognition : BaseEntity
{
    public int AuthorId { get; set; }
    public int RecipientId { get; set; }
    public int AwardedPoints { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Narrative { get; set; } = string.Empty;
    public string? Badge { get; set; }
    public string Status { get; set; } = "Published";
    public DateTime? ScheduledFor { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public Employee? Author { get; set; }
    public Employee? Recipient { get; set; }
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
}
