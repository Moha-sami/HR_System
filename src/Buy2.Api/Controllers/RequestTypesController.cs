using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.CreateRequestType;
using Buy2.Application.Features.Requests.DeleteRequestType;
using Buy2.Application.Features.Requests.GetRequestTypeById;
using Buy2.Application.Features.Requests.GetRequestTypes;
using Buy2.Application.Features.Requests.UpdateRequestType;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/request-types")]
[Route("api/v1/requestTypes")]
[Authorize]
public class RequestTypesController : ControllerBase
{
    private readonly ISender _mediator;

    public RequestTypesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all request types with optional search, category, and active status filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RequestTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequestTypes(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRequestTypesQuery(search, category, isActive), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single request type by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RequestTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRequestTypeById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetRequestTypeByIdQuery(id), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new request type.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RequestTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRequestType(
        [FromBody] CreateRequestTypeDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateRequestTypeCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(GetRequestTypeById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing request type.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RequestTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRequestType(
        [FromRoute] int id,
        [FromBody] UpdateRequestTypeDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new UpdateRequestTypeCommand(id, dto), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a request type by ID if not referenced by existing requests.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRequestType(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteRequestTypeCommand(id), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is ValidationException or InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
