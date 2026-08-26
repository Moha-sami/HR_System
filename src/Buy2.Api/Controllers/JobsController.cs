using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobById;
using Buy2.Application.Features.Jobs.GetJobEmployees;
using Buy2.Application.Features.Jobs.GetJobs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]
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
}
