using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.DeleteEmployee;
using Buy2.Application.Features.Employees.ExportEmployees;
using Buy2.Application.Features.Employees.GetEmployee;
using Buy2.Application.Features.Employees.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeeDirectoryController : ControllerBase
{
    private readonly ISender _mediator;

    public EmployeeDirectoryController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedEmployeeListDto>> GetEmployees([FromQuery] GetEmployeesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployeeProfileDto>> GetEmployeeProfile(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEmployeeProfileQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportEmployees([FromQuery] ExportEmployeesQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return File(result, "text/csv", "employees.csv");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteEmployeeCommand(id), cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}

