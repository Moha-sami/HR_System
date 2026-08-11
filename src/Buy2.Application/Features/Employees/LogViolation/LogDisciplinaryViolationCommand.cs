using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;


namespace Buy2.Application.Features.Employees.LogViolation;

public record LogDisciplinaryViolationCommand(
    int EmployeeId,
    string Severity,
    string Description
    ) : IRequest<int>;

public class LogDisciplinaryViolationCommandHandler : IRequestHandler<LogDisciplinaryViolationCommand, int>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<DisciplinaryViolation> _disciplinaryViolationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogDisciplinaryViolationCommandHandler
        (
            IRepository<Employee> employeeRepository,
            IRepository<DisciplinaryViolation> disciplinaryViolationRepository,
            IUnitOfWork unitOfWork
        )
    {
        _employeeRepository = employeeRepository;
        _disciplinaryViolationRepository = disciplinaryViolationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(LogDisciplinaryViolationCommand command, CancellationToken cancellationToken)
    {
        var employeeId = await _employeeRepository.GetByIdAsync(command.EmployeeId);
        if(employeeId is null)
        {
            throw new ValidationException("Employee not found!");
        }

        var violation = new DisciplinaryViolation
        {
            EmployeeId = command.EmployeeId,
            Severity = command.Severity,
            Description = command.Description
        };

        await _disciplinaryViolationRepository.AddAsync(violation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return violation.Id;
    }

}