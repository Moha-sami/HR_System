using Buy2.Application.Features.ShiftMarket.ClaimShift;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-market/claims/{id}")]
[Authorize]
public class ShiftClaimsController : ControllerBase
{
    private readonly ISender _mediator;
    public ShiftClaimsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> Create(int id, [FromBody] ClaimShiftCommand command)
    {
        if (command.ShiftId != id)
        {
            return BadRequest("Shift Not Match");
        }
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}