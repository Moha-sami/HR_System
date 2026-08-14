using Buy2.Application.Features.Sites.CreateSite;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/sites")]
[Authorize(Roles = "Admin")]
public class CreateSiteController : ControllerBase
{
    private readonly ISender _mediator;
    public CreateSiteController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> Create([FromBody] CreateSiteCommand command)
    {
        var siteId = await _mediator.Send(command);

        return Created($"/api/v1/sites/{siteId}", siteId);
    }
}