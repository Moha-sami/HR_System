using Buy2.Application.Features.ShiftTemplates;
using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Sites.GetSiteShiftTemplates;

public static class SiteShiftTemplateMapper
{
    public static SiteShiftTemplateDto ToDto(ShiftTemplate template)
    {
        var blocks = template.ShiftBlocks
            .OrderBy(b => b.Id)
            .Select(b => new SiteShiftTemplateBlockDto(
                b.Id,
                ShiftTimeHelper.FormatTime(b.StartTime),
                ShiftTimeHelper.FormatTime(b.EndTime),
                b.JobRoleId,
                b.EmployeeId))
            .ToList();

        return new SiteShiftTemplateDto(
            template.Id,
            template.Name,
            ShiftTimeHelper.FormatTime(template.StartTime),
            ShiftTimeHelper.FormatTime(template.EndTime),
            blocks.Count,
            blocks);
    }
}
