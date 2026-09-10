using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.GetSiteShiftTemplates;

public record SiteShiftTemplateBlockDto(
    int Id,
    string StartTime,
    string EndTime,
    int JobRoleId,
    int? AssignedUserId
);

public record SiteShiftTemplateDto(
    int Id,
    string Name,
    string StartTime,
    string EndTime,
    int TotalBlockCount,
    List<SiteShiftTemplateBlockDto> ShiftBlocks
);

public record GetSiteShiftTemplatesQuery(int SiteId, string? Search)
    : IRequest<Result<List<SiteShiftTemplateDto>>>;

public class GetSiteShiftTemplatesQueryHandler
    : IRequestHandler<GetSiteShiftTemplatesQuery, Result<List<SiteShiftTemplateDto>>>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;

    public GetSiteShiftTemplatesQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftTemplate> shiftTemplateRepository)
    {
        _siteRepository = siteRepository;
        _shiftTemplateRepository = shiftTemplateRepository;
    }

    public async Task<Result<List<SiteShiftTemplateDto>>> Handle(
        GetSiteShiftTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var siteExists = await _siteRepository
            .Query()
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.SiteId, cancellationToken);

        if (!siteExists)
        {
            return Result<List<SiteShiftTemplateDto>>.NotFound(
                $"Site with ID {request.SiteId} was not found.");
        }

        IQueryable<ShiftTemplate> query = _shiftTemplateRepository.Query()
            .AsNoTracking()
            .Where(t => t.ShiftTemplateSites.Any(l => l.SiteId == request.SiteId));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(normalizedSearch));
        }

        var templates = await query
            .Include(t => t.ShiftBlocks)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var items = templates.Select(SiteShiftTemplateMapper.ToDto).ToList();

        return Result<List<SiteShiftTemplateDto>>.Success(items);
    }
}
