using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Employees.UpdatePersonalInfo;

public record UpdateEmployeePersonalInfoCommand(int EmployeeId, UpdateEmployeePersonalInfoDto Dto) : IRequest<bool>;

public class UpdateEmployeePersonalInfoCommandHandler : IRequestHandler<UpdateEmployeePersonalInfoCommand, bool>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmployeePersonalInfoCommandHandler(
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateEmployeePersonalInfoCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee is null || employee.IsDeleted)
        {
            return false;
        }

        var dto = request.Dto;

        if (dto.FirstName is not null)
        {
            employee.FirstName = dto.FirstName;
        }

        if (dto.LastName is not null)
        {
            employee.LastName = dto.LastName;
        }

        if (dto.PhoneNumber is not null)
        {
            employee.PhoneNumber = dto.PhoneNumber;
        }

        if (dto.DateOfBirth.HasValue)
        {
            employee.Birthdate = dto.DateOfBirth.Value.UtcDateTime;
        }

        if (dto.Address is not null)
        {
            employee.Address = dto.Address;
        }

        if (dto.EmergencyContact is not null)
        {
            employee.EmergencyContact = dto.EmergencyContact;
        }

        if (dto.NationalId is not null)
        {
            employee.NationalId = dto.NationalId;
        }

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
