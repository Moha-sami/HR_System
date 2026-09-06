using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Qualifications.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Qualifications.CreateQualification;

public record CreateQualificationCommand(CreateQualificationDto Dto) : IRequest<QualificationDto>;

public class CreateQualificationCommandHandler : IRequestHandler<CreateQualificationCommand, QualificationDto>
{
    private readonly IRepository<Qualification> _qualificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQualificationCommandHandler(IRepository<Qualification> qualificationRepository, IUnitOfWork unitOfWork)
    {
        _qualificationRepository = qualificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<QualificationDto> Handle(CreateQualificationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _qualificationRepository.AnyAsync(q => q.Name == request.Dto.Name, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"A qualification with the name '{request.Dto.Name}' already exists.");
        }

        var qualification = new Qualification
        {
            Name = request.Dto.Name,
            Category = request.Dto.Type,
            Description = request.Dto.Description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _qualificationRepository.AddAsync(qualification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new QualificationDto(
            Id: qualification.Id,
            Name: qualification.Name,
            Type: qualification.Category,
            Description: qualification.Description,
            IsActive: qualification.IsActive
        );
    }
}
