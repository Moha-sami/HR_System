using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Commands.ToggleReaction;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/news")]
[Authorize]
public class ReactionsController : ControllerBase
{
    private readonly ISender _mediator;

    public ReactionsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Toggles an emoji reaction on a post, comment, or recognition with mutually exclusive replacement.
    /// </summary>
    [HttpPost("{targetType}/{targetId:int}/reactions")]
    [ProducesResponseType(typeof(ReactionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleReaction(
        [FromRoute] string targetType,
        [FromRoute] int targetId,
        [FromBody] ToggleReactionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int? currentUserId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var command = new ToggleReactionCommand(
                TargetType: targetType,
                TargetId: targetId,
                ReactionType: request?.ReactionType ?? string.Empty,
                CallerId: currentUserId
            );

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
