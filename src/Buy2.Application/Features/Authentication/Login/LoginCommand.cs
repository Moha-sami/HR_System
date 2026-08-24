using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Authentication.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto?>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IRepository<Employee> employeeRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _employeeRepository = employeeRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponseDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return null;
        }

        var employee = await _employeeRepository.Query()
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Email == request.Email.Trim(), cancellationToken);

        if (employee is null)
        {
            return null;
        }

        string roleName = employee.Role?.Name ?? "Employee";

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
