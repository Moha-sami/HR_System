using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        var isAscending = string.Equals(filter.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = isAscending
            ? query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            : query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);

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
}
