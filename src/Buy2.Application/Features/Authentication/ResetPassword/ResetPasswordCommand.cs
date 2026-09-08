using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Authentication.ResetPassword;

public record ResetPasswordCommand(string Email, string NewPassword) : IRequest<bool>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IRepository<Employee> _employeeRepository;

    public ResetPasswordCommandHandler(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employee = employees.FirstOrDefault(e => e.Email == request.Email);

        if (employee is null)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        return true;
    }
}
