using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.ExportRequestsHistory;
using Buy2.Application.Features.Requests.GetRequestsHistory;
using Buy2.Application.Features.Requests.GetSubmittedRequests;
using Buy2.Application.Features.Requests.ProcessDecision;
using Buy2.Application.Features.Requests.SubmitRequest;
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

    /// <summary>
    /// Queries historical resolved requests audit ledger (Approved or Rejected) with multi-column sorting and filtering.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PaginatedListResult<RequestHistorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequestsHistory(
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] List<int>? requestTypeIds,
        [FromQuery] string? overallStatus,
        [FromQuery] string? sortBy = "SubmittedAt",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRequestsHistoryQuery(
            Search: search,
            FromDate: fromDate,
            ToDate: toDate,
            RequestTypeIds: requestTypeIds,
            OverallStatus: overallStatus,
            SortBy: sortBy,
            SortDescending: sortDescending,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Exports the historical requests audit ledger to CSV or Excel (.xlsx) formats with applied filters.
    /// </summary>
    [HttpGet("history/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportRequestsHistory(
        [FromQuery] string? format = "csv",
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] List<int>? requestTypeIds = null,
        [FromQuery] string? overallStatus = null,
        [FromQuery] string? sortBy = "SubmittedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new ExportRequestsHistoryQuery(
            Format: format ?? "csv",
            Search: search,
            FromDate: fromDate,
            ToDate: toDate,
            RequestTypeIds: requestTypeIds,
            OverallStatus: overallStatus,
            SortBy: sortBy,
            SortDescending: sortDescending
        );

        var result = await _mediator.Send(query, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Submits a new employee workplace request with category attributes and file attachments.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SubmittedRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitRequest(
        [FromForm] SubmitRequestModel model,
        [FromForm] List<IFormFile>? attachments,
        CancellationToken cancellationToken)
    {
        try
        {
            var employeeId = model.EmployeeId;
            if (!employeeId.HasValue)
            {
                var rawId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(rawId, out var parsedId))
                {
                    employeeId = parsedId;
                }
            }

            if (!employeeId.HasValue || employeeId.Value <= 0)
            {
                return BadRequest(new { message = "Employee ID could not be identified from request or authentication token." });
            }

            var command = new SubmitRequestCommand(
                EmployeeId: employeeId.Value,
                RequestTypeId: model.RequestTypeId,
                StartDate: model.StartDate,
                EndDate: model.EndDate,
                Reason: model.Reason,
                CategoryValuesJson: model.CategoryValuesJson,
                Attachments: attachments
            );

            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is System.ComponentModel.DataAnnotations.ValidationException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Submits a new employee workplace request via JSON.
    /// </summary>
    [HttpPost("json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SubmittedRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitRequestJson(
        [FromBody] SubmitRequestModel model,
        CancellationToken cancellationToken)
    {
        return await SubmitRequest(model, null, cancellationToken);
    }

    /// <summary>
    /// Retrieves full details of a specific request including attachments, dual review notes, and prior submission history.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestDetail(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new Buy2.Application.Features.Requests.GetRequestDetail.GetRequestDetailQuery(id), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Processes a manager or HR review decision (Approval or Rejection) on an employee workplace request.
    /// </summary>
    [HttpPost("{id:int}/decision")]
    [ProducesResponseType(typeof(ProcessDecisionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessDecision(
        [FromRoute] int id,
        [FromBody] ProcessDecisionModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            var reviewerId = model.ReviewerId;
            if (!reviewerId.HasValue)
            {
                var rawId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(rawId, out var parsedId))
                {
                    reviewerId = parsedId;
                }
            }

            var command = new ProcessRequestDecisionCommand(
                RequestId: id,
                Tier: model.Tier,
                Decision: model.Decision,
                Comment: model.Comment,
                RejectionReason: model.RejectionReason,
                ReviewerId: reviewerId
            );

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is System.ComponentModel.DataAnnotations.ValidationException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}



