using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.ShiftTemplates.DeleteShiftTemplate;

public record DeleteShiftTemplateCommand(int Id) : IRequest<Result>;

public class DeleteShiftTemplateCommandHandler
    : IRequestHandler<DeleteShiftTemplateCommand, Result>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteShiftTemplateCommandHandler(
        IRepository<ShiftTemplate> shiftTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteShiftTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _shiftTemplateRepository.GetByIdAsync(request.Id, cancellationToken);

        if (template is null)
        {
            return Result.NotFound($"Shift template with ID {request.Id} was not found.");
        }
        
        _shiftTemplateRepository.Delete(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
