namespace Buy2.Domain.Entities;

public class ShiftTemplate : BaseEntity
{
    public int? OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int? LastUpdatedByEmployeeId { get; set; }

    public Organization? Organization { get; set; }
    public Employee? LastUpdatedByEmployee { get; set; }
    public ICollection<ShiftBlock> ShiftBlocks { get; set; } = new List<ShiftBlock>();
    public ICollection<ShiftTemplateSite> ShiftTemplateSites { get; set; } = new List<ShiftTemplateSite>();
}
