using Buy2.Application.Features.Authentication.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthLoginController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthLoginController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
