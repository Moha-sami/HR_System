using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Features.Rewards.Commands;
using Buy2.Application.Features.Rewards.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop.Infrastructure;

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
    [ProducesResponseType(typeof(RewardProfileListDto),StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RewardProfileListDto>> CreateReward([FromForm] RewardCreateDto dto, CancellationToken cancellation)
    {
        CreateRewardCommand command = new CreateRewardCommand(dto);
        var createReward = await _mediator.Send(command, cancellation);
        return Created($"api/v1/rewards/{createReward}", createReward);
    }

}