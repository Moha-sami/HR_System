namespace Buy2.Domain.Entities;

public class ShiftTemplateSite : BaseEntity
{
    public int ShiftTemplateId { get; set; }
    public int SiteId { get; set; }

    public ShiftTemplate? ShiftTemplate { get; set; }
    public Site? Site { get; set; }
}
