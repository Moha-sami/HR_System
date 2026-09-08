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
            ShiftTimeHelper.FormatDate(template.CreatedAt),
            template.UpdatedAt.HasValue
                ? ShiftTimeHelper.FormatDate(template.UpdatedAt.Value)
                : ShiftTimeHelper.FormatDate(template.CreatedAt),
            template.LastUpdatedByEmployeeId,
            ShiftTimeHelper.FormatTime(template.StartTime),
            ShiftTimeHelper.FormatTime(template.EndTime),
            template.ShiftTemplateSites
                .OrderBy(s => s.SiteId)
                .Select(s => new ShiftTemplateSiteRefDto(
                    s.SiteId,
                    s.Site?.SiteName ?? string.Empty))
                .ToList(),
            template.ShiftBlocks
                .OrderBy(b => b.Id)
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
