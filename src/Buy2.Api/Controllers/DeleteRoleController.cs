using Buy2.Application.Features.Roles.DeleteRole;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
public class DeleteRoleController : ControllerBase
{
    private readonly ISender _mediator;

    public DeleteRoleController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var deleted = await _mediator.Send(new DeleteRoleCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}