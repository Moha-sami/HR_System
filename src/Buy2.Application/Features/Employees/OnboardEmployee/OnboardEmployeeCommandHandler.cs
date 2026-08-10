using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Employees.OnboardEmployee;

public class OnboardEmployeeCommandHandler : IRequestHandler<OnboardEmployeeCommand, int>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OnboardEmployeeCommandHandler(IRepository<Employee> employeeRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _jobRoleRepository = jobRoleRepository;
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(OnboardEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetAllAsync();

        if(employees.Any(e => e.Email == command.Email))
        {
            throw new ValidationException($"Employee with email {command.Email} already exists!");
        }

        var jobRole = await _jobRoleRepository.GetByIdAsync(command.JobRoleId);
        if (jobRole is null)
        {
            throw new ValidationException("Job Role Not Found!");
        }

        var site = await _siteRepository.GetByIdAsync(command.SiteId);
        if (site is null) 
        { 
            throw new ValidationException("Site Not Found!");
        }

        var employee = new Employee
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            JobRoleId = command.JobRoleId,
            SiteId = command.SiteId
        };

        await _employeeRepository.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}