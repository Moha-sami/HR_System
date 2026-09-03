using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.CreateManualPointsTransaction;
using Buy2.Application.Features.Points.GetPointsTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buy2.Api.Controllers;

[ApiController]
[Route("api/v1/points/transactions")]
[Authorize]
public class PointsTransactionsController : ControllerBase
{
    private readonly ISender _mediator;

    public PointsTransactionsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,HR,SuperAdmin")]
    [ProducesResponseType(typeof(PaginatedPointsTransactionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedPointsTransactionsResponseDto>> GetPointsTransactions(
        [FromQuery] PointsTransactionFilterQueryDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPointsTransactionsQuery(filter);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,SuperAdmin")]
    [ProducesResponseType(typeof(CreateManualPointsTransactionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateManualPointsTransactionResult>> CreateManualPointsTransaction(
        [FromBody] CreateManualPointsTransactionDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateManualPointsTransactionCommand(
            EmployeeId: dto.EmployeeId,
            TransactionType: dto.TransactionType,
            PointsValue: dto.PointsValue,
            Comments: dto.Comments);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsNotFound)
        {
            return NotFound(result.ErrorMessage);
        }

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }
}