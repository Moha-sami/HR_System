using Buy2.Application.Features.Employees.LogViolation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees/{id}/violations")]

public class DisciplinaryViolationsController : ControllerBase
{
    private readonly ISender _mediator;
    public DisciplinaryViolationsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> LogViolation(int id, LogDisciplinaryViolationCommand command)
    {
        if(id != command.EmployeeId)
        {
            return BadRequest("Employee ID mismatch.");
        }

        var violationId = await _mediator.Send(command);

        return Ok(violationId);
    }
}