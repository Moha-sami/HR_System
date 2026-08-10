using Buy2.Application.Features.Authentication.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthPasswordResetController : ControllerBase
{
    private readonly ISender _mediator;

    public AuthPasswordResetController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
