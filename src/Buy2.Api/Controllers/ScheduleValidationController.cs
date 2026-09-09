using Buy2.Application.DTOs;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.CommitCopyShifts;
using Buy2.Application.Features.Schedules.PreflightCopyShifts;
using Buy2.Application.Features.Schedules.ValidateDraft;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/schedules/validate-draft")]
[Authorize(Roles = "Admin,Manager,OperationsManager")]
public class ScheduleValidationController : ControllerBase
{
    private readonly ISender _mediator;

    public ScheduleValidationController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(PreFlightValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PreFlightValidationResultDto>> ValidateDraft([FromBody] List<DraftShiftDto> shifts, CancellationToken cancellationToken)
    {
        var command = new ValidateScheduleDraftCommand(shifts);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("/api/v1/shifts/copy/preflight")]
    [Authorize(Roles = "Admin,Manager,OperationsManager")]
    [ProducesResponseType(typeof(PreflightCopyShiftsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PreflightCopyShiftsResponseDto>> PreflightCopyShifts(
        [FromBody] PreflightCopyShiftsRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new PreflightCopyShiftsQuery(
                request.SiteId,
                request.SourceDate,
                request.TargetDates,
                request.RecurringDays,
                request.WeekCount);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("/api/v1/shifts/copy/commit")]
    [Authorize(Roles = "Admin,Manager,OperationsManager")]
    [ProducesResponseType(typeof(CommitCopyShiftsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommitCopyShiftsResponseDto>> CommitCopyShifts(
        [FromBody] CommitCopyShiftsRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CommitCopyShiftsCommand(
                request.SiteId,
                request.SourceDate,
                request.TargetDates,
                request.DateResolutions,
                request.BulkReplaceAll,
                request.CopyAssignments);

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}