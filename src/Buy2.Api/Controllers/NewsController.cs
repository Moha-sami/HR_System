using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.CreateNewsPost;
using Buy2.Application.Features.News.GetNewsFeed;
using Buy2.Application.Features.News.GetNewsPostDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/news")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly ISender _mediator;

    public NewsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Browses a paginated feed of corporate news posts with engagement metrics, status filters, and keyword search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedListResult<NewsFeedSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNewsFeed(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var isElevated = User.IsInRole("Admin") ||
                         User.IsInRole("HR") ||
                         User.IsInRole("SuperAdmin") ||
                         User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin"));

        var query = new GetNewsFeedQuery(
            Search: search,
            Category: category,
            Status: status,
            IsElevatedUser: isElevated,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets detailed news post with full article content, author metadata, reaction breakdown, and engagement counters.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NewsPostDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNewsPostDetail([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var isElevated = User.IsInRole("Admin") ||
                             User.IsInRole("HR") ||
                             User.IsInRole("SuperAdmin") ||
                             User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin"));

            int? currentUserId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var query = new GetNewsPostDetailQuery(
                Id: id,
                CurrentUserId: currentUserId,
                IsElevatedUser: isElevated
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
    /// Creates a corporate news post with draft, scheduled, or published lifecycle status and optional media attachment.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(NewsPostResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNewsPost(
        [FromForm] CreateNewsPostModel model,
        IFormFile? mediaFile,
        CancellationToken cancellationToken)
    {
        try
        {
            var authorId = model.AuthorId;
            if (!authorId.HasValue)
            {
                var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(rawId, out var parsedId))
                {
                    authorId = parsedId;
                }
            }

            var file = mediaFile ?? model.MediaFile;

            var command = new CreateNewsPostCommand(
                Title: model.Title,
                Content: model.Content,
                Category: model.Category ?? "General",
                Status: model.Status ?? "Draft",
                ScheduledFor: model.ScheduledFor,
                AuthorId: authorId,
                MediaFile: file
            );

            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
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
    /// Updates an existing news post's content, media attachment, scheduling parameters, and lifecycle status.
    /// </summary>
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(NewsPostResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNewsPost(
        [FromRoute] int id,
        [FromForm] UpdateNewsPostModel model,
        IFormFile? mediaFile,
        CancellationToken cancellationToken)
    {
        try
        {
            int? modifierId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                modifierId = parsedId;
            }

            var file = mediaFile ?? model.MediaFile;

            var command = new Buy2.Application.Features.News.UpdateNewsPost.UpdateNewsPostCommand(
                Id: id,
                Title: model.Title,
                Content: model.Content,
                Category: model.Category ?? "General",
                Status: model.Status ?? "Draft",
                ScheduledFor: model.ScheduledFor,
                ModifierId: modifierId,
                MediaFile: file
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

public class CreateNewsPostModel
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; set; } = "General";

    [MaxLength(30)]
    public string? Status { get; set; } = "Draft";

    public DateTime? ScheduledFor { get; set; }

    public int? AuthorId { get; set; }

    public IFormFile? MediaFile { get; set; }
}

public class UpdateNewsPostModel
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; set; } = "General";

    [MaxLength(30)]
    public string? Status { get; set; } = "Draft";

    public DateTime? ScheduledFor { get; set; }

    public IFormFile? MediaFile { get; set; }
}

