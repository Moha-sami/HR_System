using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobById;
using Buy2.Application.Features.Jobs.GetJobEmployees;
using Buy2.Application.Features.Jobs.GetJobs;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = "HRAdmin,Admin,Manager,HR,SuperAdmin")]
public class JobsController : ControllerBase
{
    private readonly ISender _mediator;

    public JobsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(JobPaginatedResponseDto<JobListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetJobs([FromQuery] JobFilterQueryDto filter, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetJobsQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(JobDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetJobById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetJobByIdQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Job role with ID {id} was not found." });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}/employees")]
    [ProducesResponseType(typeof(JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetJobEmployees(
        [FromRoute] int id,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetJobEmployeesQuery(id, searchTerm, pageNumber, pageSize), cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Job role with ID {id} was not found." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateJobCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(GetJobById), new { id = result.Id }, result);
        }
        catch (System.InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    public async Task<IActionResult> UpdateJob([FromRoute] int id, [FromBody] UpdateJobDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new UpdateJobCommand(id, dto), cancellationToken);
            return Ok(result);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("{id:int}/deletion-impact")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(JobDeletionImpactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobDeletionImpact([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetJobDeletionImpactQuery(id), cancellationToken);
            return Ok(result);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reassign-and-delete")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignAndDeleteJob([FromRoute] int id, [FromBody] ReassignEmployeesAndDeleteJobDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new ReassignAndDeleteJobCommand(id, dto.ReplacementJobId), cancellationToken);
            return NoContent();
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
