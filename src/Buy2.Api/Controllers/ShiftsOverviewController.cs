using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.AssignEmployeeToShift;
using Buy2.Application.Features.Schedules.AutoFillDailyShifts;
using Buy2.Application.Features.Schedules.CreateShiftBlock;
using Buy2.Application.Features.Schedules.GetDailyShiftSchedule;
using Buy2.Application.Features.Schedules.GetSiteShiftsOverview;
using Buy2.Application.Features.Schedules.UnassignOrDeleteShiftBlock;
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

    [HttpGet("/api/v1/shifts/daily")]
    [ProducesResponseType(typeof(DailyShiftScheduleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyShiftScheduleResponseDto>> GetDailyShiftSchedule(
        [FromQuery] int siteId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDailyShiftScheduleQuery(siteId, date);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("/api/v1/shifts/blocks")]
    [ProducesResponseType(typeof(ShiftBlockResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShiftBlockResponseDto>> CreateShiftBlock(
        [FromBody] CreateShiftBlockRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateShiftBlockCommand(
            dto.SiteId,
            dto.Date,
            dto.StartTime,
            dto.EndTime,
            dto.JobRoleId,
            dto.DispatchPolicy);
        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/v1/shifts/blocks/{result.ShiftId}", result);
    }

    [HttpPost("/api/v1/shifts/blocks/{id}/assign")]
    [ProducesResponseType(typeof(AssignEmployeeToShiftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignEmployeeToShiftResponseDto>> AssignEmployee(
        [FromRoute] int id,
        [FromBody] AssignEmployeeToShiftRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new AssignEmployeeToShiftCommand(
            id,
            dto.EmployeeId,
            dto.ConfirmOverride,
            dto.OverrideReason);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("/api/v1/shifts/blocks/{id}/assign")]
    [ProducesResponseType(typeof(UnassignOrDeleteShiftBlockResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnassignOrDeleteShiftBlockResponseDto>> UnassignOrDeleteShiftBlock(
        [FromRoute] int id,
        [FromQuery] ShiftBlockRemovalAction action = ShiftBlockRemovalAction.UnassignOnly,
        [FromQuery] bool confirmPublishedDeletion = false,
        CancellationToken cancellationToken = default)
    {
        var command = new UnassignOrDeleteShiftBlockCommand(id, action, confirmPublishedDeletion);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("/api/v1/shifts/auto-fill")]
    [ProducesResponseType(typeof(AutoFillDailyShiftsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutoFillDailyShiftsResponseDto>> AutoFillDailyShifts(
        [FromBody] AutoFillDailyShiftsRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new AutoFillDailyShiftsCommand(dto.SiteId, dto.Date);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
