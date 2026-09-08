using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Departments.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Departments.CreateDepartment;

public record CreateDepartmentCommand(CreateDepartmentDto Dto) : IRequest<DepartmentDto>;

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IRepository<Department> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDepartmentCommandHandler(IRepository<Department> departmentRepository, IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var existing = await _departmentRepository.AnyAsync(d => d.Name == request.Dto.Name, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"A department with the name '{request.Dto.Name}' already exists.");
        }

        var department = new Department
        {
            Name = request.Dto.Name,
            Code = request.Dto.Code,
            Description = request.Dto.Description,
            HeadEmployeeId = request.Dto.HeadEmployeeId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(
            Id: department.Id,
            Name: department.Name,
            Code: department.Code,
            Description: department.Description,
            HeadEmployeeId: department.HeadEmployeeId,
            HeadEmployeeName: null,
            JobRolesCount: 0,
            EmployeesCount: 0,
            IsActive: department.IsActive
        );
    }
}
