using Buy2.Application.Features.Employees.BulkOnboard;
using Buy2.Application.Features.Employees.OnboardEmployee;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees/onboard")]
[Authorize(Roles = "Admin,Manager")]
public class EmployeeOnboardingController : ControllerBase
{
    private readonly ISender _mediator;
    public EmployeeOnboardingController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> Onboard(OnboardEmployeeCommand command)
    {
        var employeeId = await _mediator.Send(command);

        return Created($"/api/v1/employees/{employeeId}", employeeId);
    }

    [HttpPost("/api/v1/employees/bulk-onboard")]
    [ProducesResponseType(typeof(BulkOnboardResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkOnboardResultDto>> BulkOnboard([FromBody] BulkOnboardCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}