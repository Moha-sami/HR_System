using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.CreateRole;
using Buy2.Application.Features.Roles.GetRoleById;
using Buy2.Application.Features.Roles.GetRoles;
using Buy2.Application.Features.Roles.UpdateRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]
public class RolesController : ControllerBase
{
    private readonly ISender _mediator;

    public RolesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RolePaginatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles([FromQuery] RoleFilterQueryDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRolesQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoleById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]
    [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRoleCommand(dto), cancellationToken);
        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        return Created($"/api/v1/roles/{result.CreatedRole!.Id}", result.CreatedRole);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]
    [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRole([FromRoute] int id, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(id, dto), cancellationToken);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (result.IsForbidden)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        return Ok(result.UpdatedRole);
    }
}
