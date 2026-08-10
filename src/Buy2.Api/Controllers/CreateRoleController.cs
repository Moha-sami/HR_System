using Buy2.Application.Features.Roles.CreateRole;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
public class CreateRoleController : ControllerBase
{
    private readonly ISender _mediator;

    public CreateRoleController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var roleId = await _mediator.Send(command);
        return Created($"api/v1/roles/{roleId}", roleId);
    }
}
