using Buy2.Application.Features.Employees.LogViolation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees/{id}/violations")]
[Authorize(Roles = "Admin,Manager")]
public class DisciplinaryViolationsController : ControllerBase
{
    private readonly ISender _mediator;
    public DisciplinaryViolationsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> LogViolation(int id, [FromBody] LogDisciplinaryViolationCommand command)
    {
        if (id != command.EmployeeId)
        {
            return BadRequest("Employee ID mismatch.");
        }

        var violationId = await _mediator.Send(command);

        return Ok(violationId);
    }
}