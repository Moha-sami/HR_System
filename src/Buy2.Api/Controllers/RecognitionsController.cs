using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Recognitions.Commands.DeleteRecognition;
using Buy2.Application.Features.Recognitions.Commands.UpdateRecognition;
using Buy2.Application.Features.Recognitions.Queries.GetRecognitionDetail;
using Buy2.Application.Features.Recognitions.Queries.GetRecognitions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/recognitions")]
[Authorize]
public class RecognitionsController : ControllerBase
{
    private readonly ISender _mediator;

    public RecognitionsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Queries the paginated directory of employee recognitions with search, status filters, and multi-column sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedListResult<RecognitionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecognitions(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var isElevated = User.IsInRole("Admin") ||
                         User.IsInRole("HR") ||
                         User.IsInRole("SuperAdmin") ||
                         User.IsInRole("Manager") ||
                         User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin" || c.Value == "Manager"));

        var query = new GetRecognitionsQuery(
            Search: search,
            Status: status,
            SortBy: sortBy,
            SortDirection: sortDirection,
            PageNumber: pageNumber,
            PageSize: pageSize,
            IsElevatedUser: isElevated
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed representation of a specific recognition post including recipient profile, points, and audit trail.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RecognitionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecognitionById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var isElevated = User.IsInRole("Admin") ||
                             User.IsInRole("HR") ||
                             User.IsInRole("SuperAdmin") ||
                             User.IsInRole("Manager") ||
                             User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin" || c.Value == "Manager"));

            int? currentUserId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var query = new GetRecognitionDetailQuery(
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
    /// Creates an employee recognition with recipient allocation, points award, and optional attachment.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Buy2.Application.DTOs.Recognitions.RecognitionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRecognition(
        [FromForm] CreateRecognitionModel model,
        IFormFile? attachmentFile,
        CancellationToken cancellationToken = default)
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

            var file = attachmentFile ?? model.AttachmentFile;

            var command = new Buy2.Application.Features.Recognitions.Commands.CreateRecognition.CreateRecognitionCommand(
                RecipientId: model.RecipientId,
                Title: model.Title,
                Narrative: model.Narrative,
                AwardedPoints: model.AwardedPoints,
                Badge: model.Badge,
                Status: model.Status ?? "Published",
                ScheduledFor: model.ScheduledFor,
                AuthorId: authorId,
                AttachmentFile: file
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
    /// Updates recognition content, points award, recipient, attachments, and scheduling.
    /// </summary>
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Buy2.Application.DTOs.Recognitions.RecognitionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRecognition(
        int id,
        [FromForm] UpdateRecognitionModel model,
        IFormFile? attachmentFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isElevated = User.IsInRole("Admin") ||
                             User.IsInRole("HR") ||
                             User.IsInRole("SuperAdmin") ||
                             User.IsInRole("Manager") ||
                             User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin" || c.Value == "Manager"));

            int? currentUserId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var file = attachmentFile ?? model.AttachmentFile;

            var command = new UpdateRecognitionCommand(
                Id: id,
                RecipientId: model.RecipientId,
                Title: model.Title,
                Narrative: model.Narrative,
                AwardedPoints: model.AwardedPoints,
                Badge: model.Badge,
                Status: model.Status,
                ScheduledFor: model.ScheduledFor,
                ModifyingUserId: currentUserId,
                IsElevatedUser: isElevated,
                AttachmentFile: file
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
    /// Soft-deletes a recognition post with points grant reversal safeguard.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecognition(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var isElevated = User.IsInRole("Admin") ||
                             User.IsInRole("HR") ||
                             User.IsInRole("SuperAdmin") ||
                             User.IsInRole("Manager") ||
                             User.HasClaim(c => c.Type == ClaimTypes.Role && (c.Value == "Admin" || c.Value == "HR" || c.Value == "SuperAdmin" || c.Value == "Manager"));

            int? currentUserId = null;
            var rawId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(rawId, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var command = new DeleteRecognitionCommand(
                Id: id,
                CurrentUserId: currentUserId,
                IsElevatedUser: isElevated
            );

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}

public class CreateRecognitionModel
{
    public int RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Narrative { get; set; } = string.Empty;
    public int AwardedPoints { get; set; } = 0;
    public string? Badge { get; set; }
    public string? Status { get; set; } = "Published";
    public DateTime? ScheduledFor { get; set; }
    public int? AuthorId { get; set; }
    public IFormFile? AttachmentFile { get; set; }
}

public class UpdateRecognitionModel
{
    public int RecipientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Narrative { get; set; } = string.Empty;
    public int AwardedPoints { get; set; } = 0;
    public string? Badge { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public IFormFile? AttachmentFile { get; set; }
}
