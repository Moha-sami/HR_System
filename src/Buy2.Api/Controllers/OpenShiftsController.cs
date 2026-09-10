using Buy2.Application.Features.ShiftMarket.GetOpenShifts;
using Buy2.Application.Features.ShiftMarket.GetShiftMarketPostings;
using Buy2.Application.DTOs;
using Buy2.Application.DTOs.ShiftMarket;
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

    [HttpGet("/api/v1/shifts/market")]
    [Authorize(Roles = "Admin,Manager,OperationsManager")]
    [ProducesResponseType(typeof(ShiftMarketPaginatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShiftMarketPaginatedResponseDto>> GetShiftMarketPostings(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetShiftMarketPostingsQuery(status, search, page, pageSize, siteId), cancellationToken);
        return Ok(result);
    }
}
