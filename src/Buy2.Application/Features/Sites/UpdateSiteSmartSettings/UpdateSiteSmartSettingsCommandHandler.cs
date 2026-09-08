using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Sites.UpdateSiteSmartSettings;

public class UpdateSiteSmartSettingsCommandHandler : IRequestHandler<UpdateSiteSmartSettingsCommand, SiteSmartSettingsResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSiteSmartSettingsCommandHandler(
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SiteSmartSettingsResponseDto> Handle(UpdateSiteSmartSettingsCommand request, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.GetByIdAsync(request.SiteId);
        if (site is null)
        {
            throw new KeyNotFoundException($"Site with ID {request.SiteId} not found.");
        }

        if (request.IsSmartAssignmentEnabled.HasValue)
        {
            site.IsSmartAssignmentEnabled = request.IsSmartAssignmentEnabled.Value;
        }

        if (request.IsSmartPostingEnabled.HasValue)
        {
            site.IsSmartPostingEnabled = request.IsSmartPostingEnabled.Value;
        }

        _siteRepository.Update(site);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SiteSmartSettingsResponseDto(site.Id, site.IsSmartAssignmentEnabled, site.IsSmartPostingEnabled);
    }
}
