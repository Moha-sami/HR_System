using Buy2.Application.Features.Points.CreateRule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/points/rules")]
[Authorize(Roles = "Admin")]
public class PointsRulesController : ControllerBase
{
    private readonly ISender _mediator;

    public PointsRulesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> CreateRule([FromBody] CreatePointsRuleCommand command, CancellationToken cancellationToken)
    {
        var ruleId = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/points/rules/{ruleId}", ruleId);
    }
}
