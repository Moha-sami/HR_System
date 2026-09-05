using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.Automation.ExecuteAutomationNow;
using Buy2.Application.Features.Points.Automation.SaveAutomationSettings;
using Buy2.Application.Features.Points.GetAutomationRules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/points")]
[Authorize]
public class PointsController : ControllerBase
{
    private readonly ISender _mediator;

    public PointsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("automation")]
    [Authorize(Roles = "Admin,HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(PointsAutomationOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PointsAutomationOverviewDto>> GetAutomationRules(CancellationToken cancellationToken)
    {
        var query = new GetPointsAutomationRulesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPost("automation/execute")]
    [Authorize(Roles = "Admin,HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(AutomationJobResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutomationJobResultDto>> ExecuteAutomationNow(
        [FromBody] ExecuteAutomationNowDto request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ExecuteAutomationNowCommand(request), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
            {
                return NotFound(new { message = result.ErrorMessage });
            }

            return BadRequest(new { message = result.ErrorMessage });
        }

        if (result.Data is null)
        {
            return Ok(new { message = result.Message ?? "Nothing to evaluate." });
        }

        return Ok(result.Data);
    }

    [HttpPut("automation")]
    [Authorize(Roles = "Admin,HRAdmin,SuperAdmin")]
    [ProducesResponseType(typeof(PointsAutomationOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PointsAutomationOverviewDto>> SaveAutomationSettings(
        [FromBody] SaveAutomationSettingsDto request,
        CancellationToken cancellationToken)
    {
        var saveResult = await _mediator.Send(new SaveAutomationSettingsCommand(request), cancellationToken);

        if (!saveResult.IsSuccess)
        {
            return BadRequest(new { message = saveResult.ErrorMessage });
        }

        var queryResult = await _mediator.Send(new GetPointsAutomationRulesQuery(), cancellationToken);

        if (!queryResult.IsSuccess || queryResult.Data is null)
        {
            return Ok(new { savedCount = saveResult.SavedCount });
        }

        return Ok(queryResult.Data);
    }
}