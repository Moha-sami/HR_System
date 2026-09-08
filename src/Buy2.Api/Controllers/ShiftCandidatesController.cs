using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.Candidates;
using Buy2.Application.Features.Schedules.GetEmployeeShiftPreview;
using Buy2.Application.Features.Schedules.GetShiftCandidateEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shifts/candidates")]
[Authorize(Roles = "Admin,Manager,OperationsManager")]
public class ShiftCandidatesController : ControllerBase
{
    private readonly ISender _mediator;

    public ShiftCandidatesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedShiftCandidatesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedShiftCandidatesResponseDto>> GetShiftCandidates(
        [FromQuery] GetShiftCandidatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/v1/shifts/employees")]
    [ProducesResponseType(typeof(PaginatedShiftCandidateEmployeesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedShiftCandidateEmployeesResponseDto>> GetShiftCandidateEmployees(
        [FromQuery] GetShiftCandidateEmployeesQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/preview")]
    [ProducesResponseType(typeof(ShiftCandidatePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShiftCandidatePreviewDto>> GetCandidatePreview(
        [FromRoute] int id,
        [FromQuery] int? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetShiftCandidatePreviewQuery(id, siteId);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Shift candidate with ID {id} was not found." });
        }
        return Ok(result);
    }

    [HttpGet("/api/v1/shifts/employees/{employeeId:int}/preview")]
    [Authorize(Roles = "Admin,Manager,OperationsManager")]
    [ProducesResponseType(typeof(ShiftCandidatePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShiftCandidatePreviewDto>> GetEmployeeShiftPreview(
        [FromRoute] int employeeId,
        [FromQuery] int? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployeeShiftPreviewQuery(employeeId, siteId);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Employee with ID {employeeId} was not found." });
        }
        return Ok(result);
    }
}

