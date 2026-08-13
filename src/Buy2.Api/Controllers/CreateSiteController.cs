using Buy2.Application.Features.Sites.CreateSite;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/sites")]

public class CreateSiteController : ControllerBase
{
    private readonly ISender _mediator;
    public CreateSiteController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateSiteCommand command)
    {
        var siteId = await _mediator.Send(command);

        return Ok(siteId);
    }
}