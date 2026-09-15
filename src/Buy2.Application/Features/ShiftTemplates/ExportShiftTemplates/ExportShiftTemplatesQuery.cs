using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Common.Specifications;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.ShiftTemplates.ExportShiftTemplates;

public record ExportShiftTemplatesQuery(ShiftTemplateFilterQueryDto Filter)
    : IRequest<Result<byte[]>>;

public class ExportShiftTemplatesQueryHandler
    : IRequestHandler<ExportShiftTemplatesQuery, Result<byte[]>>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;

    public ExportShiftTemplatesQueryHandler(IRepository<ShiftTemplate> shiftTemplateRepository)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
    }

    public async Task<Result<byte[]>> Handle(
        ExportShiftTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter is null
            ? new ShiftTemplateFilterQueryDto()
            : request.Filter with { PageNumber = 1, PageSize = 10 };

        var validation = await new ShiftTemplateFilterQueryDtoValidator().ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<byte[]>.ValidationFailure(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var spec = new Specification<ShiftTemplate>()
            .Include(
                "ShiftTemplateSites",
                "ShiftTemplateSites.Site",
                "ShiftBlocks",
                "ShiftBlocks.JobRole",
                "ShiftBlocks.Employee");
        ApplySearchFilter(spec, filter);
        ApplySorting(spec, filter);

        var templates = await _shiftTemplateRepository.ListAsync(spec, cancellationToken);

        var rows = templates
            .Select(t => new ShiftTemplateExportRow(
                t.Id,
                t.Name,
                t.StartTime,
                t.EndTime,
                t.CreatedAt,
                t.UpdatedAt,
                t.LastUpdatedByEmployeeId,
                t.ShiftTemplateSites
                    .Select(s => new ShiftTemplateExportSiteRow(
                        s.SiteId,
                        s.Site != null ? s.Site.SiteName : string.Empty))
                    .ToList(),
                t.ShiftBlocks
                    .Select(b => new ShiftTemplateExportBlockRow(
                        b.Id,
                        b.StartTime,
                        b.EndTime,
                        b.JobRoleId,
                        b.JobRole != null ? b.JobRole.Title : string.Empty,
                        b.EmployeeId,
                        b.Employee != null ? b.Employee.FirstName : null,
                        b.Employee != null ? b.Employee.LastName : null))
                    .ToList()))
            .ToList();

        var details = rows.Select(ToDetailsDto).ToList();
        var bytes = ShiftTemplateExportWorkbookBuilder.Build(details);

        return Result<byte[]>.Success(bytes);
    }

    private static ShiftTemplateDetailsDto ToDetailsDto(ShiftTemplateExportRow row)
    {
        return new ShiftTemplateDetailsDto(
            row.Id,
            row.Name,
            ShiftTimeHelper.FormatTime(row.StartTime),
            ShiftTimeHelper.FormatTime(row.EndTime),
            ShiftTimeHelper.FormatDate(row.CreatedAt),
            row.UpdatedAt.HasValue
                ? ShiftTimeHelper.FormatDate(row.UpdatedAt.Value)
                : ShiftTimeHelper.FormatDate(row.CreatedAt),
            row.Sites.Count,
            row.Sites
                .Select(s => new ShiftTemplateSiteItemDto(s.SiteId, s.SiteName))
                .ToList(),
            row.Blocks
                .Select(b => new ShiftTemplateBlockDetailsDto(
                    b.Id,
                    ShiftTimeHelper.FormatTime(b.StartTime),
                    ShiftTimeHelper.FormatTime(b.EndTime),
                    b.JobRoleId,
                    b.JobRoleTitle,
                    b.EmployeeId,
                    BuildEmployeeName(b.EmployeeFirstName, b.EmployeeLastName)))
                .ToList(),
            row.LastUpdatedByEmployeeId);
    }

    private static string BuildEmployeeName(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return string.Empty;
        }

        return $"{firstName} {lastName}".Trim();
    }

    private static void ApplySearchFilter(
        Specification<ShiftTemplate> spec,
        ShiftTemplateFilterQueryDto filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            return;
        }

        var searchTerm = filter.SearchTerm.Trim();
        spec.Where(t => t.Name.Contains(searchTerm));
    }

    private static void ApplySorting(
        Specification<ShiftTemplate> spec,
        ShiftTemplateFilterQueryDto filter)
    {
        var specs = BuildSortSpecs(filter);

        var first = true;
        foreach (var sortSpec in specs)
        {
            ApplySortSpec(spec, sortSpec, ref first);
        }

        if (specs[0].Ascending)
        {
            spec.ThenBy(t => t.Id);
        }
        else
        {
            spec.ThenBy(t => t.Id, descending: true);
        }
    }

    private static List<ShiftTemplateSortSpec> BuildSortSpecs(ShiftTemplateFilterQueryDto filter)
    {
        var creationRaw = FirstSentValue(filter.CreationSort, filter.SortDir);
        var specs = new List<ShiftTemplateSortSpec>(4)
        {
            new(ShiftTemplateSortKey.Creation, IsAscending(creationRaw), HasValue(creationRaw)),
            new(ShiftTemplateSortKey.Updated, IsAscending(filter.UpdatedSort), HasValue(filter.UpdatedSort)),
            new(ShiftTemplateSortKey.Name, IsAscending(filter.NameSort), HasValue(filter.NameSort)),
            new(ShiftTemplateSortKey.Assigned, IsAscending(filter.NumberOfAssignedSort), HasValue(filter.NumberOfAssignedSort)),
        };

        return specs.OrderByDescending(s => s.Explicit).ToList();
    }

    private static void ApplySortSpec(
        Specification<ShiftTemplate> spec,
        ShiftTemplateSortSpec sortSpec,
        ref bool first)
    {
        switch (sortSpec.Key)
        {
            case ShiftTemplateSortKey.Creation:
                if (first) { spec.OrderBy(t => t.CreatedAt, descending: !sortSpec.Ascending); }
                else { spec.ThenBy(t => t.CreatedAt, descending: !sortSpec.Ascending); }
                break;
            case ShiftTemplateSortKey.Updated:
                if (first) { spec.OrderBy(t => t.UpdatedAt!, descending: !sortSpec.Ascending); }
                else { spec.ThenBy(t => t.UpdatedAt!, descending: !sortSpec.Ascending); }
                break;
            case ShiftTemplateSortKey.Name:
                if (first) { spec.OrderBy(t => t.Name, descending: !sortSpec.Ascending); }
                else { spec.ThenBy(t => t.Name, descending: !sortSpec.Ascending); }
                break;
            default:
                if (first) { spec.OrderBy(t => t.ShiftTemplateSites.Count, descending: !sortSpec.Ascending); }
                else { spec.ThenBy(t => t.ShiftTemplateSites.Count, descending: !sortSpec.Ascending); }
                break;
        }

        first = false;
    }

    private static bool IsAscending(string? sortDir)
    {
        return string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasValue(string? sortDir)
    {
        return !string.IsNullOrWhiteSpace(sortDir);
    }

    private static string? FirstSentValue(string? primary, string? fallback)
    {
        return HasValue(primary) ? primary : fallback;
    }

    private enum ShiftTemplateSortKey
    {
        Creation,
        Updated,
        Name,
        Assigned
    }

    private sealed record ShiftTemplateSortSpec(ShiftTemplateSortKey Key, bool Ascending, bool Explicit);

    private sealed record ShiftTemplateExportSiteRow(int SiteId, string SiteName);

    private sealed record ShiftTemplateExportBlockRow(
        int Id,
        TimeSpan StartTime,
        TimeSpan EndTime,
        int JobRoleId,
        string JobRoleTitle,
        int? EmployeeId,
        string? EmployeeFirstName,
        string? EmployeeLastName);

    private sealed record ShiftTemplateExportRow(
        int Id,
        string Name,
        TimeSpan StartTime,
        TimeSpan EndTime,
        DateTime CreatedAt,
        DateTimeOffset? UpdatedAt,
        int? LastUpdatedByEmployeeId,
        List<ShiftTemplateExportSiteRow> Sites,
        List<ShiftTemplateExportBlockRow> Blocks);
}
