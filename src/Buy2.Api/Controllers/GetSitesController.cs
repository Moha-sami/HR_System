using Buy2.Application.CQRS.Authentication.Sites.Query;
using Buy2.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/sites")]
public class GetSitesController : ControllerBase
{
    private readonly ISender _mediator;

    public GetSitesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<SiteDto>>> GetSites(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSitesQuery(), cancellationToken);
        return Ok(result);
    }
}
