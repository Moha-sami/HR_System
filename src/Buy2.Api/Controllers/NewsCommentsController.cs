using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.GetPostComments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/news")]
[Authorize]
public class NewsCommentsController : ControllerBase
{
    private readonly ISender _mediator;

    public NewsCommentsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Queries threaded comments and nested replies for a news post with aggregate reactions and caller state.
    /// </summary>
    [HttpGet("{postId:int}/comments")]
    [ProducesResponseType(typeof(PaginatedListResult<CommentThreadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostComments(
        [FromRoute] int postId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
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

            var query = new GetPostCommentsQuery(
                PostId: postId,
                CurrentUserId: currentUserId,
                PageNumber: pageNumber,
                PageSize: pageSize
            );

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Adds a comment or threaded reply to a news post with author attribution and count increment.
    /// </summary>
    [HttpPost("{postId:int}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateComment(
        [FromRoute] int postId,
        [FromBody] CreateCommentDto request,
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

            var command = new Buy2.Application.Features.News.Commands.CreateComment.CreateCommentCommand(
                PostId: postId,
                Content: request?.Content ?? string.Empty,
                ParentCommentId: request?.ParentCommentId,
                AuthorId: currentUserId
            );

            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
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

    /// <summary>
    /// Updates a comment content by its author with validation and last-modified timestamp tracking.
    /// </summary>
    [HttpPut("comments/{commentId:int}")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComment(
        [FromRoute] int commentId,
        [FromBody] UpdateCommentDto request,
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

            var command = new Buy2.Application.Features.News.Commands.UpdateComment.UpdateCommentCommand(
                CommentId: commentId,
                Content: request?.Content ?? string.Empty,
                CallerId: currentUserId
            );

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    /// <summary>
    /// Deletes a comment by author or moderates inappropriate content by administrator.
    /// </summary>
    [HttpDelete("comments/{commentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] int commentId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isElevated = User.IsInRole("Admin") ||
                             User.IsInRole("HR") ||
                             User.IsInRole("SuperAdmin") ||
                             User.IsInRole("Moderator") ||
                             User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin" || c.Value == "Moderator"));

            int? currentUserId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var command = new Buy2.Application.Features.News.Commands.DeleteComment.DeleteCommentCommand(
                CommentId: commentId,
                CallerId: currentUserId,
                IsElevatedUser: isElevated,
                ModerationReason: reason
            );

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { message = "Comment successfully deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
