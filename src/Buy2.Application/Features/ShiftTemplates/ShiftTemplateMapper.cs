using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Domain.Entities;

namespace Buy2.Application.Features.ShiftTemplates;

public static class ShiftTemplateMapper
{
    public static ShiftTemplateDetailsDto ToDetailsDto(ShiftTemplate template)
    {
        return new ShiftTemplateDetailsDto(
            template.Id,
            template.Name,
            ShiftTimeHelper.FormatTime(template.StartTime),
            ShiftTimeHelper.FormatTime(template.EndTime),
            ShiftTimeHelper.FormatDate(template.CreatedAt),
            template.UpdatedAt.HasValue
                ? ShiftTimeHelper.FormatDate(template.UpdatedAt.Value)
                : ShiftTimeHelper.FormatDate(template.CreatedAt),
            template.ShiftTemplateSites.Count,
            template.ShiftTemplateSites
                .Select(s => new ShiftTemplateSiteItemDto(
                    s.SiteId,
                    s.Site?.SiteName ?? string.Empty))
                .ToList(),
            template.ShiftBlocks
                .Select(b => new ShiftTemplateBlockDetailsDto(
                    b.Id,
                    ShiftTimeHelper.FormatTime(b.StartTime),
                    ShiftTimeHelper.FormatTime(b.EndTime),
                    b.JobRoleId,
                    b.JobRole?.Title ?? string.Empty,
                    b.EmployeeId,
                    BuildEmployeeName(b.Employee)))
                .ToList()
        );
    }

    private static string BuildEmployeeName(Employee? employee)
    {
        if (employee is null)
        {
            return string.Empty;
        }

        return $"{employee.FirstName} {employee.LastName}".Trim();
    }
}
