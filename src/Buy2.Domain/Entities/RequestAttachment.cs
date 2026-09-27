using System;

namespace Buy2.Domain.Entities;

public class RequestAttachment : BaseEntity
{
    public int RequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Request? Request { get; set; }
}
