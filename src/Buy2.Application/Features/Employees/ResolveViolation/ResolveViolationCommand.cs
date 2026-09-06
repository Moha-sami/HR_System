using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.ResolveViolation;

public record ResolveViolationResult(bool IsSuccess, bool IsNotFound, bool IsBadRequest = false, string? ErrorMessage = null)
{
    public static ResolveViolationResult Success() => new(true, false, false);
    public static ResolveViolationResult NotFound(string message = "Resource not found.") => new(false, true, false, message);
    public static ResolveViolationResult BadRequest(string message) => new(false, false, true, message);
}

public record ResolveViolationCommand(int EmployeeId, int ViolationId, ResolveViolationDto Dto) : IRequest<ResolveViolationResult>;

public class ResolveViolationCommandHandler : IRequestHandler<ResolveViolationCommand, ResolveViolationResult>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<DisciplinaryViolation> _disciplinaryViolationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResolveViolationCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<DisciplinaryViolation> disciplinaryViolationRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _disciplinaryViolationRepository = disciplinaryViolationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResolveViolationResult> Handle(ResolveViolationCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null || employee.IsDeleted)
        {
            return ResolveViolationResult.NotFound($"Employee with ID {request.EmployeeId} was not found.");
        }

        var violation = await _disciplinaryViolationRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(v => v.Id == request.ViolationId && v.EmployeeId == request.EmployeeId, cancellationToken);

        if (violation is null)
        {
            return ResolveViolationResult.NotFound($"Disciplinary violation with ID {request.ViolationId} was not found for employee {request.EmployeeId}.");
        }

        if (violation.Status == ViolationStatus.Resolved)
        {
            return ResolveViolationResult.BadRequest("Violation is already resolved.");
        }

        violation.Status = ViolationStatus.Resolved;
        violation.ActionType = request.Dto.ActionType;
        violation.ActionDate = request.Dto.ActionDate ?? DateTime.UtcNow;
        violation.ActionDescription = request.Dto.ActionDescription;
        violation.ActionTakenById = request.Dto.ActionTakenById;

        _disciplinaryViolationRepository.Update(violation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ResolveViolationResult.Success();
    }
}
