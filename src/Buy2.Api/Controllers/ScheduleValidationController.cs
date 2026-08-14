using Buy2.Application.DTOs;
using Buy2.Application.Features.Schedules.ValidateDraft;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/schedules/validate-draft")]
[Authorize(Roles = "Admin,Manager")]
public class ScheduleValidationController : ControllerBase
{
    private readonly ISender _mediator;

    public ScheduleValidationController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PreFlightValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PreFlightValidationResultDto>> ValidateDraft([FromBody] List<DraftShiftDto> shifts)
    {
        var command = new ValidateScheduleDraftCommand(shifts);
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}