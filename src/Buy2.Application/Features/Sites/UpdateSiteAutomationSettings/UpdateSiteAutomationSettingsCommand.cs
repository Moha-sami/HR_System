using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Sites.UpdateSiteAutomationSettings;

public record UpdateSiteAutomationSettingsCommand(
    int SiteId,
    bool? IsSmartAssignmentEnabled,
    bool? IsSmartPostingEnabled
) : IRequest<bool>;

public class UpdateSiteAutomationSettingsCommandHandler : IRequestHandler<UpdateSiteAutomationSettingsCommand, bool>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSiteAutomationSettingsCommandHandler(
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateSiteAutomationSettingsCommand request, CancellationToken cancellationToken)
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

        return true;
    }
}
