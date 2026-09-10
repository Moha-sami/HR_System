using Buy2.Application.DTOs.ShiftMarket;
using Buy2.Application.Features.ShiftMarket.ApproveShiftClaim;
using Buy2.Application.Features.ShiftMarket.ClaimShift;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/shift-market/claims/{id}")]
[Authorize]
public class ShiftClaimsController : ControllerBase
{
    private readonly ISender _mediator;
    public ShiftClaimsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> Create(int id, [FromBody] ClaimShiftCommand command, CancellationToken cancellationToken)
    {
        if (command.ShiftId != id)
        {
            return BadRequest("Shift Not Match");
        }
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("/api/v1/shifts/market/claims/{claimId}/approve")]
    [Authorize(Roles = "Admin,Manager,OperationsManager")]
    [ProducesResponseType(typeof(ApproveShiftClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApproveShiftClaimResponseDto>> Approve(int claimId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new ApproveShiftClaimCommand(claimId), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}