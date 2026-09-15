using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Common.Specifications;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.ShiftTemplates.GetShiftTemplates;

public record GetShiftTemplatesQuery(ShiftTemplateFilterQueryDto Filter)
    : IRequest<Result<ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>>>;

public class GetShiftTemplatesQueryHandler
    : IRequestHandler<GetShiftTemplatesQuery, Result<ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>>>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;

    public GetShiftTemplatesQueryHandler(IRepository<ShiftTemplate> shiftTemplateRepository)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
    }

    public async Task<Result<ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>>> Handle(
        GetShiftTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new ShiftTemplateFilterQueryDto();

        var validation = await new ShiftTemplateFilterQueryDtoValidator().ValidateAsync(filter, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>>.ValidationFailure(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var page = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var spec = new Specification<ShiftTemplate>()
            .Include(nameof(ShiftTemplate.ShiftTemplateSites));

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim();
            spec.Where(t => t.Name.Contains(searchTerm));
        }

        ApplySorting(spec, filter);

        var paged = await _shiftTemplateRepository.PagedAsync(spec, page, pageSize, cancellationToken);

        var items = paged.Items.Select(t => new ShiftTemplateListItemDto(
            t.Id,
            t.Name,
            ShiftTimeHelper.FormatDate(t.CreatedAt),
            t.UpdatedAt.HasValue
                ? ShiftTimeHelper.FormatDate(t.UpdatedAt.Value)
                : ShiftTimeHelper.FormatDate(t.CreatedAt),
            t.ShiftTemplateSites.Count
        )).ToList();

        return Result<ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>>.Success(
            new ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>(items, paged.TotalCount, page, pageSize, paged.TotalPages));
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
        // Legacy SortDir is kept as a fallback for the creation sort only.
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
}
