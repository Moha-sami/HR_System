using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

        IQueryable<ShiftTemplate> query = ApplySearchFilter(_shiftTemplateRepository.Query(), filter);
        query = ApplySorting(query, filter);

        var rows = await query
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
            .ToListAsync(cancellationToken);

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

    private static IQueryable<ShiftTemplate> ApplySearchFilter(
        IQueryable<ShiftTemplate> query,
        ShiftTemplateFilterQueryDto filter)
    {
        if (string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            return query;
        }

        var searchTerm = filter.SearchTerm.Trim();
        return query.Where(t => t.Name.Contains(searchTerm));
    }

    private static IQueryable<ShiftTemplate> ApplySorting(
        IQueryable<ShiftTemplate> query,
        ShiftTemplateFilterQueryDto filter)
    {
        var specs = BuildSortSpecs(filter);

        IOrderedQueryable<ShiftTemplate>? ordered = null;
        foreach (var spec in specs)
        {
            ordered = ApplySortSpec(query, ordered, spec);
        }

        return specs[0].Ascending
            ? ordered!.ThenBy(t => t.Id)
            : ordered!.ThenByDescending(t => t.Id);
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

    private static IOrderedQueryable<ShiftTemplate> ApplySortSpec(
        IQueryable<ShiftTemplate> query,
        IOrderedQueryable<ShiftTemplate>? ordered,
        ShiftTemplateSortSpec spec)
    {
        return spec.Key switch
        {
            ShiftTemplateSortKey.Creation => OrderWith(query, ordered, t => t.CreatedAt, spec.Ascending),
            ShiftTemplateSortKey.Updated => OrderWith(query, ordered, t => t.UpdatedAt, spec.Ascending),
            ShiftTemplateSortKey.Name => OrderWith(query, ordered, t => t.Name, spec.Ascending),
            _ => OrderWith(query, ordered, t => t.ShiftTemplateSites.Count, spec.Ascending),
        };
    }

    private static IOrderedQueryable<ShiftTemplate> OrderWith<TKey>(
        IQueryable<ShiftTemplate> query,
        IOrderedQueryable<ShiftTemplate>? ordered,
        Expression<Func<ShiftTemplate, TKey>> keySelector,
        bool ascending)
    {
        if (ordered is null)
        {
            return ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        }

        return ascending ? ordered.ThenBy(keySelector) : ordered.ThenByDescending(keySelector);
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
