using Buy2.Application.Features.ShiftMarket.ClaimShift;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-market/claims/{id}")]
public class ShiftClaimsController : ControllerBase
{
    private readonly ISender _mediator;
    public ShiftClaimsController(ISender mediator)
    {
        _mediator = mediator;
    }


    [HttpPost]
    public async Task<ActionResult<bool>> Create(int id, ClaimShiftCommand command)
    {
        if(command.ShiftId != id)
        {
            return BadRequest("Shift Not Match");
        }
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}