using Buy2.Application.Features.Employees.OnboardEmployee;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees/onboard")]
public class EmployeeOnboardingController : ControllerBase
{
    private readonly ISender _mediator;
    public EmployeeOnboardingController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Onboard(OnboardEmployeeCommand command)
    {
        var employeeId = await _mediator.Send(command);

        return Ok(employeeId);
    }
}
