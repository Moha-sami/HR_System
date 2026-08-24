using Buy2.Application.Features.ShiftMarket.GetOpenShifts;
using Buy2.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-market")]
[Authorize]
public class OpenShiftsController : ControllerBase
{
    private readonly ISender _mediator;

    public OpenShiftsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("open-shifts")]
    [ProducesResponseType(typeof(List<ShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ShiftDto>>> GetOpenShifts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOpenShiftsQuery(), cancellationToken);
        return Ok(result);
    }
}
