using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using System.Runtime.CompilerServices;

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
        var name = command.Name.Trim();
        var regionExists = await _regionRepository
            .AnyAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

        if (regionExists)
        {
            throw new ValidationException("Region name already exists!");
        }

        var region = new Region { Name = name };

        await _regionRepository.AddAsync(region);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return region.Id;
    }
}