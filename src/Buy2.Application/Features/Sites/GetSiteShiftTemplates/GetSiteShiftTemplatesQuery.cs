using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Common.Specifications;
using Buy2.Domain.Entities;
using MediatR;

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

public record GetSiteShiftTemplatesQuery(
    int SiteId,
    string? Search,
    int? ActorEmployeeId = null,
    bool BypassSiteAccess = false)
    : IRequest<Result<List<SiteShiftTemplateDto>>>;

public class GetSiteShiftTemplatesQueryHandler
    : IRequestHandler<GetSiteShiftTemplatesQuery, Result<List<SiteShiftTemplateDto>>>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IRepository<EmployeeSite> _employeeSiteRepository;

    public GetSiteShiftTemplatesQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftTemplate> shiftTemplateRepository,
        IRepository<EmployeeSite> employeeSiteRepository)
    {
        _siteRepository = siteRepository;
        _shiftTemplateRepository = shiftTemplateRepository;
        _employeeSiteRepository = employeeSiteRepository;
    }

    public async Task<Result<List<SiteShiftTemplateDto>>> Handle(
        GetSiteShiftTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var siteExists = await _siteRepository
            .AnyAsync(s => s.Id == request.SiteId, cancellationToken);

        if (!siteExists)
        {
            return Result<List<SiteShiftTemplateDto>>.NotFound(
                $"Site with ID {request.SiteId} was not found.");
        }

        if (!request.BypassSiteAccess && !await HasSiteAccessAsync(request, cancellationToken))
        {
            return Result<List<SiteShiftTemplateDto>>.Forbidden(
                "You do not have access to this site.");
        }

        var spec = new Specification<ShiftTemplate>()
            .Where(t => t.ShiftTemplateSites.Any(l => l.SiteId == request.SiteId))
            .Include(nameof(ShiftTemplate.ShiftBlocks))
            .OrderBy(t => t.Id);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim().ToLower();
            spec.Where(t => t.Name.ToLower().Contains(normalizedSearch));
        }

        var templates = await _shiftTemplateRepository.ListAsync(spec, cancellationToken);

        var items = templates.Select(SiteShiftTemplateMapper.ToDto).ToList();

        return Result<List<SiteShiftTemplateDto>>.Success(items);
    }

    private async Task<bool> HasSiteAccessAsync(
        GetSiteShiftTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.ActorEmployeeId.HasValue)
        {
            return false;
        }

        return await _employeeSiteRepository
            .AnyAsync(
                es => es.EmployeeId == request.ActorEmployeeId.Value
                    && es.SiteId == request.SiteId,
                cancellationToken);
    }
}
