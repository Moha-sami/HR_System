using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

        IQueryable<ShiftTemplate> query = _shiftTemplateRepository.Query()
            .AsNoTracking()
            .Include(t => t.ShiftTemplateSites);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim();
            query = query.Where(t => t.Name.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, filter);

        var templates = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = templates.Select(t => new ShiftTemplateListItemDto(
            t.Id,
            t.Name,
            ShiftTimeHelper.FormatDate(t.CreatedAt),
            t.UpdatedAt.HasValue
                ? ShiftTimeHelper.FormatDate(t.UpdatedAt.Value)
                : ShiftTimeHelper.FormatDate(t.CreatedAt),
            t.ShiftTemplateSites.Count
        )).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        return Result<ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>>.Success(
            new ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>(items, totalCount, page, pageSize, totalPages));
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
}
