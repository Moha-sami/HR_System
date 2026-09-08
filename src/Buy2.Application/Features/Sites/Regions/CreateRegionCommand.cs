using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Buy2.Application.Features.Sites.Regions;
public record CreateRegionCommand(string Name) : IRequest<int>;
public class CreateRegionCommandHandler : IRequestHandler<CreateRegionCommand, int>
{
    private readonly IRepository<Region> _regionRepository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateRegionCommandHandler(IRepository<Region> regionRepository, IUnitOfWork unitOfWork)
    {
        _regionRepository = regionRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task<int> Handle(CreateRegionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ValidationException("Region name is required.");
        }

        var name = command.Name.Trim();
        var regionExists = await _regionRepository
            .AnyAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

        if (regionExists)
        {
            throw new ValidationException("Region name already exists!");
        }

        var region = new Region { Name = name, IsActive = true };

        await _regionRepository.AddAsync(region, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return region.Id;
    }
}