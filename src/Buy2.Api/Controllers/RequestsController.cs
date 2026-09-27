using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.GetSubmittedRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly ISender _mediator;

    public RequestsController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Queries submitted employee requests with multi-criteria filters, search, and dynamic sorting.
    /// </summary>
    [HttpGet("submitted")]
    [ProducesResponseType(typeof(PaginatedListResult<SubmittedRequestSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmittedRequests(
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] List<int>? requestTypeIds,
        [FromQuery] string? managerStatus,
        [FromQuery] string? hrStatus,
        [FromQuery] string? overallStatus,
        [FromQuery] string? sortBy = "SubmittedAt",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSubmittedRequestsQuery(
            Search: search,
            FromDate: fromDate,
            ToDate: toDate,
            RequestTypeIds: requestTypeIds,
            ManagerStatus: managerStatus,
            HrStatus: hrStatus,
            OverallStatus: overallStatus,
            SortBy: sortBy,
            SortDescending: sortDescending,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
