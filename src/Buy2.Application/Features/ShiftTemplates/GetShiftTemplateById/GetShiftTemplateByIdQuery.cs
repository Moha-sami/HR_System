using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var template = await _shiftTemplateRepository.Query()
            .AsNoTracking()
            .Include(t => t.ShiftTemplateSites)
                .ThenInclude(s => s.Site)
            .Include(t => t.ShiftBlocks)
                .ThenInclude(b => b.JobRole)
            .Include(t => t.ShiftBlocks)
                .ThenInclude(b => b.Employee)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template is null)
        {
            return Result<ShiftTemplateDetailsDto>.NotFound(
                $"Shift template with ID {request.Id} was not found.");
        }

        return Result<ShiftTemplateDetailsDto>.Success(ShiftTemplateMapper.ToDetailsDto(template));
    }
}
