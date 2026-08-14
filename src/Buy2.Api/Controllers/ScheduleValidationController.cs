using Buy2.Application.DTOs;
using Buy2.Application.Features.Schedules.ValidateDraft;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/schedules/validate-draft")]
public class ScheduleValidationController : ControllerBase
{
    private readonly ISender _mediator;

    public ScheduleValidationController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<PreFlightValidationResultDto>> ValidateDraft(List<DraftShiftDto> shifts)
    {
        var command = new ValidateScheduleDraftCommand(shifts);
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}