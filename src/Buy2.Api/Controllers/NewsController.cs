using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.GetNewsFeed;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
}
