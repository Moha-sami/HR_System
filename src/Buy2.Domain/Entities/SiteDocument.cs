namespace Buy2.Domain.Entities;
public class SiteDocument : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
}