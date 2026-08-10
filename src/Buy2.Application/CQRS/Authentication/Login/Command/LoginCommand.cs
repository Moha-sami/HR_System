using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Authentication.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<Role> roleRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetAllAsync();
        var employee = employees.FirstOrDefault(e => e.Email == request.Email);

        if (employee is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        // Fetch user role name
        var role = await _roleRepository.GetByIdAsync(employee.RoleId);

        string roleName;
        if (role != null)
        {
            roleName = role.Name;
        }
        else
        {
            roleName = "Employee";
        }

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(employee.Id.ToString(), employee.Email, roleName);

        return new LoginResponseDto
        {
            Token = token,
            UserId = employee.Id,
            Email = employee.Email,
            Role = roleName
        };
    }
}
