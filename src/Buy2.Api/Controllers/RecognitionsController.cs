using Buy2.Application.DTOs.Requests;
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
}
