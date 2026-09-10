using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.UpdateShiftTemplate;

public record UpdateShiftTemplateCommand(int Id, UpdateShiftTemplateDto Dto, int? ActorEmployeeId)
    : IRequest<Result<ShiftTemplateDetailsDto>>;

public class UpdateShiftTemplateCommandHandler
    : IRequestHandler<UpdateShiftTemplateCommand, Result<ShiftTemplateDetailsDto>>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly ShiftTemplateUpdateService _updateService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShiftTemplateCommandHandler(
        IRepository<ShiftTemplate> shiftTemplateRepository,
        ShiftTemplateUpdateService updateService,
        IUnitOfWork unitOfWork)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
        _updateService = updateService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ShiftTemplateDetailsDto>> Handle(
        UpdateShiftTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var shape = await new UpdateShiftTemplateDtoValidator().ValidateAsync(request.Dto, cancellationToken);
        if (!shape.IsValid)
        {
            return Result<ShiftTemplateDetailsDto>.ValidationFailure(
                string.Join("; ", shape.Errors.Select(e => e.ErrorMessage)));
        }

        var validated = await _updateService.ValidateAsync(request.Id, request.Dto, cancellationToken);
        if (!validated.IsSuccess)
        {
            return PropagateFailure(validated);
        }

        try
        {
            var details = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                _updateService.Apply(validated.Value!, request.ActorEmployeeId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var updated = await _shiftTemplateRepository.Query()
                    .AsNoTracking()
                    .Include(t => t.ShiftTemplateSites)
                        .ThenInclude(s => s.Site)
                    .Include(t => t.ShiftBlocks)
                        .ThenInclude(b => b.JobRole)
                    .Include(t => t.ShiftBlocks)
                        .ThenInclude(b => b.Employee)
                    .FirstAsync(t => t.Id == request.Id, cancellationToken);

                return ShiftTemplateMapper.ToDetailsDto(updated);
            }, cancellationToken);

            return Result<ShiftTemplateDetailsDto>.Success(details);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ShiftTemplateDetailsDto>.Conflict(
                "The shift template was modified by another request. Please reload and try again.");
        }
    }

    private static Result<ShiftTemplateDetailsDto> PropagateFailure<T>(Result<T> source) => source.ErrorType switch
    {
        ResultErrorType.NotFound => Result<ShiftTemplateDetailsDto>.NotFound(
            source.ErrorMessage ?? "Shift template was not found."),
        ResultErrorType.Conflict => Result<ShiftTemplateDetailsDto>.Conflict(
            source.ErrorMessage ?? "Shift template is in conflict."),
        _ => Result<ShiftTemplateDetailsDto>.ValidationFailure(
            source.ErrorMessage ?? "Shift template update failed validation.")
    };
}
