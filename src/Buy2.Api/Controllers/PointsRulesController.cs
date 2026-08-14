using Buy2.Application.CQRS.Points.CreateRule;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/points/rules")]
public class PointsRulesController : ControllerBase
{
    private readonly ISender _mediator;

    public PointsRulesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateRule([FromBody] CreatePointsRuleCommand command, CancellationToken cancellationToken)
    {
        var ruleId = await _mediator.Send(command, cancellationToken);
        return Ok(ruleId);
    }
}
