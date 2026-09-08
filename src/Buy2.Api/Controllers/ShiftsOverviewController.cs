using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.GetSiteShiftsOverview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,OperationsManager")]
public class ShiftsOverviewController : ControllerBase
{
    private readonly ISender _mediator;

    public ShiftsOverviewController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("/api/v1/shifts/overview")]
    [ProducesResponseType(typeof(SiteShiftsOverviewPaginatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SiteShiftsOverviewPaginatedResponseDto>> GetOverview(
        [FromQuery] string? search = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSiteShiftsOverviewQuery(search, regionId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
