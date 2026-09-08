using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.GetShiftTemplates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-templates")]
[Authorize(Roles = "HRAdmin,Admin,Manager,HR,SuperAdmin")]
public class ShiftTemplatesController : ControllerBase
{
    private readonly ISender _mediator;

    public ShiftTemplatesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetShiftTemplates(
        [FromQuery] ShiftTemplateFilterQueryDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShiftTemplatesQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }
}
