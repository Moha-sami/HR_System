using Buy2.Application.CQRS.ShiftMarket.GetOpenShifts;
using Buy2.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-market")]
public class OpenShiftsController : ControllerBase
{
    private readonly ISender _mediator;

    public OpenShiftsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("open-shifts")]
    public async Task<ActionResult<List<ShiftDto>>> GetOpenShifts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOpenShiftsQuery(), cancellationToken);
        return Ok(result);
    }
}
