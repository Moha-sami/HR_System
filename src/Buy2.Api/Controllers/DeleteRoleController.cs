using Buy2.Application.Features.Roles.DeleteRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Roles = "Admin")]
public class DeleteRoleController : ControllerBase
{
    private readonly ISender _mediator;

    public DeleteRoleController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
