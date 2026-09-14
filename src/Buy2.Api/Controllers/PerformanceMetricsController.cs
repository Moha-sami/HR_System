using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.PerformanceMetrics.CreateMetric;
using Buy2.Application.Features.Employees.PerformanceMetrics.GetMetrics;
using Buy2.Application.Features.Employees.PerformanceMetrics.UpdateMetric;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/performance/metrics")]
[Authorize]
public class PerformanceMetricsController : ControllerBase
{
    private readonly ISender _mediator;

    public PerformanceMetricsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]
    [ProducesResponseType(typeof(List<PerformanceMetricDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PerformanceMetricDto>>> GetMetrics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPerformanceMetricsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PerformanceMetricDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerformanceMetricDto>> CreateMetric(
        [FromBody] CreatePerformanceMetricDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreatePerformanceMetricCommand(dto), cancellationToken);

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PerformanceMetricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerformanceMetricDto>> UpdateMetric(
        [FromRoute] int id,
        [FromBody] UpdatePerformanceMetricDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdatePerformanceMetricCommand(id, dto), cancellationToken);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }
}
