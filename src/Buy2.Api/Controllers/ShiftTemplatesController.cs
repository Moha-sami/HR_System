using System.Security.Claims;
using Buy2.Application.Features.ShiftTemplates.CreateShiftTemplate;
using Buy2.Application.Features.ShiftTemplates.DeleteShiftTemplate;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.GetShiftTemplateById;
using Buy2.Application.Features.ShiftTemplates.GetShiftTemplates;
using Buy2.Application.Features.ShiftTemplates.UpdateShiftTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-templates")]
[Authorize(Roles = "HRAdmin,Admin,Manager,HR,SuperAdmin")]
public class ShiftTemplatesController : ControllerBase
{
    private readonly ISender _mediator;

    public ShiftTemplatesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetShiftTemplates(
        [FromQuery] ShiftTemplateFilterQueryDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShiftTemplatesQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShiftTemplateDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetShiftTemplateById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShiftTemplateByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ShiftTemplateDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShiftTemplate(
        [FromBody] CreateShiftTemplateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateShiftTemplateCommand(dto, GetActorEmployeeId()),
            cancellationToken);

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetShiftTemplateById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ShiftTemplateDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateShiftTemplate(
        [FromRoute] int id,
        [FromBody] UpdateShiftTemplateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateShiftTemplateCommand(id, dto, GetActorEmployeeId()),
            cancellationToken);

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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShiftTemplate(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteShiftTemplateCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    private int? GetActorEmployeeId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var employeeId) ? employeeId : null;
    }
}
