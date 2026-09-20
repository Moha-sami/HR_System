using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Features.Rewards.Commands;
using Buy2.Application.Features.Rewards.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/rewards")]
public class RewardsController : ControllerBase
{
    private readonly ISender _mediator;
    public RewardsController(ISender mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(PageResultDto<RewardListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PageResultDto<RewardListDto>>> GetRewards([FromQuery] GetRewardQuery query, CancellationToken cancellation)
    {
        var result = await _mediator.Send(query, cancellation);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,SuperAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(RewardProfileListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReward([FromForm] RewardCreateDto dto, IFormFile? imageFile, CancellationToken cancellation)
    {
        var result = await _mediator.Send(new CreateRewardCommand(dto, imageFile), cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteReward(int id, CancellationToken cancellation)
    {
        var result = await _mediator.Send(new DeleteRewardCommand(id), cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager,SuperAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(RewardProfileListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateReward(int id, [FromForm] RewardUpdateDto dto, IFormFile? imageFile, CancellationToken cancellation)
    {
        var result = await _mediator.Send(new UpdateRewardCommand(id, dto, imageFile), cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager,SuperAdmin")]
    [ProducesResponseType(typeof(RewardProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRewardById(int id, CancellationToken cancellation)
    {
        var result = await _mediator.Send(new GetRewardProfileQuery(id), cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id}/analytics")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(RewardAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRewardAnalytics(
        int id,
        [FromQuery] RewardTransactionFilterQueryDto filter,
        CancellationToken cancellation)
    {
        var result = await _mediator.Send(new GetRewardAnalyticsQuery(id, filter), cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id}/inventory")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PaginatedVouchersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventory(
        int id,
        [FromQuery] VoucherInventoryFilterQueryDto? dto,
        CancellationToken cancellation)
    {
        var result = await _mediator.Send(new GetRewardVoucherInventoryQuery(id, dto), cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }
    [HttpPost("{id}/inventory/upload")]
    [Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadVouchersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType( typeof(UploadVouchersResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadVouchers(int id, [FromForm] UploadVouchersRequestDto dto, CancellationToken cancellation)
    {
        var result = await _mediator.Send(
            new UploadRewardVouchersCommand(id, dto),
            cancellation);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        if (dto.Confirm)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.Value);
        }

        return Ok(result.Value);
    }
}