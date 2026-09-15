using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.ShiftTemplates.GetShiftTemplateById;

public record GetShiftTemplateByIdQuery(int Id) : IRequest<Result<ShiftTemplateDetailsDto>>;

public class GetShiftTemplateByIdQueryHandler : IRequestHandler<GetShiftTemplateByIdQuery, Result<ShiftTemplateDetailsDto>>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;

    public GetShiftTemplateByIdQueryHandler(IRepository<ShiftTemplate> shiftTemplateRepository)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
    }

    public async Task<Result<ShiftTemplateDetailsDto>> Handle(
        GetShiftTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _shiftTemplateRepository.FirstOrDefaultAsync(
            t => t.Id == request.Id,
            cancellationToken,
            "ShiftTemplateSites",
            "ShiftTemplateSites.Site",
            "ShiftBlocks",
            "ShiftBlocks.JobRole",
            "ShiftBlocks.Employee");

        if (template is null)
        {
            return Result<ShiftTemplateDetailsDto>.NotFound(
                $"Shift template with ID {request.Id} was not found.");
        }

        return Result<ShiftTemplateDetailsDto>.Success(ShiftTemplateMapper.ToDetailsDto(template));
    }
}
