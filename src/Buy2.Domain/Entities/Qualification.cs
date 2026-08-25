using System;

namespace Buy2.Domain.Entities;

public class Qualification : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public new DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
