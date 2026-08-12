using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.ShiftMarket.ClaimShift;


public record ClaimShiftCommand
    (
        int ShiftId, int EmployeeId, string OvertimeJustification
    ) : IRequest<bool>;

public class ClaimShiftCommandHandler : IRequestHandler<ClaimShiftCommand, bool>
{
    private readonly IRepository<ShiftClaim> _shiftClaimRepositroy;
    private readonly IRepository<ShiftEntity> _shiftRepository;

    private readonly IUnitOfWork _unitOfWork;

    public ClaimShiftCommandHandler(IRepository<ShiftClaim> shiftClaimRepository, IRepository<ShiftEntity> shiftEntityRepository, IUnitOfWork unitOfWork)
    {
        _shiftClaimRepositroy = shiftClaimRepository;
        _shiftRepository = shiftEntityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ClaimShiftCommand command, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(command.ShiftId);

        if (shift is null)
        {
            throw new ValidationException($"Shift not found.");
        }
        if (!shift.IsPublished)
        {
            throw new ValidationException($"Shift is not published.");
        }
        if (shift.EmployeeId is not null)
        {
            throw new ValidationException($"Shift is already claimed.");
        }

        var shiftClam = new ShiftClaim
        {
            ShiftId = command.ShiftId,
            EmployeeId = command.EmployeeId,
            Status = "Pending",
            OvertimeJustification = command.OvertimeJustification
        };

        await _shiftClaimRepositroy.AddAsync(shiftClam);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

}