using Buy2.Application.Features.Qualifications.CreateQualification;
using Buy2.Application.Features.Qualifications.DTOs;
using Buy2.Application.Features.Qualifications.GetQualifications;
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
[Route("api/v1/qualifications")]
[Authorize]
public class QualificationsController : ControllerBase
{
    private readonly ISender _mediator;

    public QualificationsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<QualificationLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQualifications([FromQuery] string? search, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQualificationsQuery(search, type), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(QualificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQualification([FromBody] CreateQualificationDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new CreateQualificationCommand(dto), cancellationToken);
            return CreatedAtAction(nameof(GetQualifications), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
